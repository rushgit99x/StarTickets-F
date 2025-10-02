using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Filters;
using StarTickets.Models;
using StarTickets.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using StarTickets.Services.Interfaces;

namespace StarTickets.Controllers
{
    [RoleAuthorize("2")] // Event Organizer only
    public class EventOrganizerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EventOrganizerController> _logger;
        private readonly Services.IPdfReportService _pdfReportService;
        private readonly IEventOrganizerService _service;

        public EventOrganizerController(
            ApplicationDbContext context,
            ILogger<EventOrganizerController> logger,
            Services.IPdfReportService pdfReportService,
            IEventOrganizerService service)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pdfReportService = pdfReportService ?? throw new ArgumentNullException(nameof(pdfReportService));
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        // GET: EventOrganizer Dashboard
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    _logger.LogWarning("Index: UserId not found in session");
                    return RedirectToAction("Login", "Auth");
                }

                var dashboardData = await _service.GetDashboardAsync(userId.Value);
                if (dashboardData == null)
                {
                    _logger.LogWarning("Index: Dashboard data is null for user {UserId}", userId.Value);
                    return View(new object()); // Return empty model
                }

                var currentUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (currentUser != null)
                {
                    ViewBag.UserName = ($"{currentUser.FirstName} {currentUser.LastName}").Trim();
                    ViewBag.UserEmail = currentUser.Email;
                }

                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard. Please try again.";
                return View(new object());
            }
        }

        // GET: EventOrganizer/Sales
        // Displays sales report for the organizer's events
        public async Task<IActionResult> Sales(DateTime? startDate, DateTime? endDate, int? eventId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    _logger.LogWarning("Sales: UserId not found in session");
                    return RedirectToAction("Login", "Auth");
                }

                // Set default date range if not provided
                DateTime start = startDate?.Date ?? DateTime.UtcNow.AddDays(-30).Date;
                DateTime end = (endDate?.Date ?? DateTime.UtcNow.Date).AddDays(1).AddTicks(-1);

                // Validate date range
                if (start > end)
                {
                    _logger.LogWarning("Sales: Invalid date range - start: {Start}, end: {End}", start, end);
                    TempData["ErrorMessage"] = "Start date cannot be after end date.";
                    start = DateTime.UtcNow.AddDays(-30).Date;
                    end = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
                }
                // Query bookings within date range for the organizer
                var bookingsInRange = _context.Bookings
                    .AsNoTracking() // Avoid tracking for performance
                    .Include(b => b.Event)
                    .Where(b => b.BookingDate >= start &&
                               b.BookingDate <= end &&
                               b.PaymentStatus == PaymentStatus.Completed &&
                               b.Event != null &&
                               b.Event.OrganizerId == userId);

                // Apply event filter if provided
                if (eventId.HasValue && eventId.Value > 0)
                {
                    bookingsInRange = bookingsInRange.Where(b => b.EventId == eventId.Value);
                }

                // Initialize view model
                var model = new ReportsViewModel
                {
                    Filter = new ReportsFilterViewModel
                    {
                        StartDate = start,
                        EndDate = end
                    }
                };

                // Calculate total sales and bookings
                model.Kpis.TotalSales = await bookingsInRange.SumAsync(b => (decimal?)b.FinalAmount) ?? 0m;
                model.Kpis.TotalBookings = await bookingsInRange.CountAsync();

                // Query bookings for today
                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1).AddTicks(-1);
                var bookingsToday = _context.Bookings
                    .AsNoTracking()
                    .Include(b => b.Event)
                    .Where(b => b.BookingDate >= today &&
                               b.BookingDate <= tomorrow &&
                               b.PaymentStatus == PaymentStatus.Completed &&
                               b.Event != null &&
                               b.Event.OrganizerId == userId);

                // Apply event filter for today's bookings
                if (eventId.HasValue && eventId.Value > 0)
                {
                    bookingsToday = bookingsToday.Where(b => b.EventId == eventId.Value);
                }

                // Calculate today's sales and bookings
                model.Kpis.SalesToday = await bookingsToday.SumAsync(b => (decimal?)b.FinalAmount) ?? 0m;
                model.Kpis.BookingsToday = await bookingsToday.CountAsync();

                // Group sales by date
                model.SalesByDate = await bookingsInRange
                    .GroupBy(b => b.BookingDate.Date)
                    .Select(g => new StarTickets.Models.ViewModels.SalesByDateItem
                    {
                        Date = g.Key,
                        Amount = g.Sum(x => x.FinalAmount)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                // Get top events by sales
                model.TopEvents = await bookingsInRange
                    .Where(b => b.Event != null)
                    .GroupBy(b => new { b.EventId, EventName = b.Event.EventName })
                    .Select(g => new StarTickets.Models.ViewModels.TopEventSalesItem
                    {
                        EventId = g.Key.EventId,
                        EventName = g.Key.EventName ?? "Unknown Event",
                        Sales = g.Sum(x => x.FinalAmount),
                        Bookings = g.Count()
                    })
                    .OrderByDescending(x => x.Sales)
                    .Take(10)
                    .ToListAsync();

                // Populate organizer events for dropdown
                ViewBag.Events = await _context.Events
                    .AsNoTracking()
                    .Where(e => e.OrganizerId == userId)
                    .OrderByDescending(e => e.EventDate)
                    .Select(e => new SelectListItem
                    {
                        Value = e.EventId.ToString(),
                        Text = e.EventName ?? "Unnamed Event"
                    })
                    .ToListAsync();
                ViewBag.SelectedEventId = eventId;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sales report");
                TempData["ErrorMessage"] = "An error occurred while loading sales data. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: EventOrganizer/DownloadSalesReport
        // Generates and downloads a sales report as PDF
        public async Task<IActionResult> DownloadSalesReport(DateTime? startDate, DateTime? endDate, int? eventId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    _logger.LogWarning("DownloadSalesReport: UserId not found in session");
                    return RedirectToAction("Login", "Auth");
                }

                // Set default date range if not provided
                DateTime start = startDate?.Date ?? DateTime.UtcNow.AddDays(-30).Date;
                DateTime end = (endDate?.Date ?? DateTime.UtcNow.Date).AddDays(1).AddTicks(-1);

                // Validate date range
                if (start > end)
                {
                    _logger.LogWarning("DownloadSalesReport: Invalid date range");
                    start = DateTime.UtcNow.AddDays(-30).Date;
                    end = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
                }

                // Query bookings within date range
                var bookingsInRange = _context.Bookings
                    .AsNoTracking()
                    .Include(b => b.Event)
                    .Where(b => b.BookingDate >= start &&
                               b.BookingDate <= end &&
                               b.PaymentStatus == PaymentStatus.Completed &&
                               b.Event != null &&
                               b.Event.OrganizerId == userId);

                // Apply event filter if provided
                if (eventId.HasValue && eventId.Value > 0)
                {
                    bookingsInRange = bookingsInRange.Where(b => b.EventId == eventId.Value);
                }

                var model = new ReportsViewModel
                {
                    Filter = new ReportsFilterViewModel
                    {
                        StartDate = start,
                        EndDate = end
                    }
                };

                // Calculate total sales and bookings
                model.Kpis.TotalSales = await bookingsInRange.SumAsync(b => (decimal?)b.FinalAmount) ?? 0m;
                model.Kpis.TotalBookings = await bookingsInRange.CountAsync();

                // Query today's bookings
                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1).AddTicks(-1);
                var bookingsToday = _context.Bookings
                    .AsNoTracking()
                    .Include(b => b.Event)
                    .Where(b => b.BookingDate >= today &&
                               b.BookingDate <= tomorrow &&
                               b.PaymentStatus == PaymentStatus.Completed &&
                               b.Event != null &&
                               b.Event.OrganizerId == userId);

                // Apply event filter for today's bookings
                if (eventId.HasValue && eventId.Value > 0)
                {
                    bookingsToday = bookingsToday.Where(b => b.EventId == eventId.Value);
                }

                // Calculate today's sales and bookings
                model.Kpis.SalesToday = await bookingsToday.SumAsync(b => (decimal?)b.FinalAmount) ?? 0m;
                model.Kpis.BookingsToday = await bookingsToday.CountAsync();

                // Group sales by date
                model.SalesByDate = await bookingsInRange
                    .GroupBy(b => b.BookingDate.Date)
                    .Select(g => new SalesByDateItem
                    {
                        Date = g.Key,
                        Amount = g.Sum(x => x.FinalAmount)
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();

                // Get top events by sales
                model.TopEvents = await bookingsInRange
                    .Where(b => b.Event != null)
                    .GroupBy(b => new { b.EventId, EventName = b.Event.EventName })
                    .Select(g => new StarTickets.Models.ViewModels.TopEventSalesItem
                    {
                        EventId = g.Key.EventId,
                        EventName = g.Key.EventName ?? "Unknown Event",
                        Sales = g.Sum(x => x.FinalAmount),
                        Bookings = g.Count()
                    })
                    .OrderByDescending(x => x.Sales)
                    .Take(10)
                    .ToListAsync();

                // Generate PDF report
                var pdfBytes = await _pdfReportService.GenerateSalesReportAsync(model);
                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    _logger.LogError("DownloadSalesReport: PDF generation returned empty result");// Log empty PDF result
                    TempData["ErrorMessage"] = "Failed to generate PDF report.";
                    return RedirectToAction(nameof(Sales), new { startDate, endDate, eventId });
                }

                var fileName = $"Organizer_Sales_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfBytes, "application/pdf", fileName);// Redirect to sales page
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sales report PDF");
                TempData["ErrorMessage"] = "An error occurred while generating the report. Please try again.";
                return RedirectToAction(nameof(Sales), new { startDate, endDate, eventId });
            }
        }

        // GET: EventOrganizer/Profile
        // Displays the organizer's profile
        public async Task<IActionResult> Profile()
        {
            try
            {
                // Verify user is logged in
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    _logger.LogWarning("Profile: UserId not found in session");
                    return RedirectToAction("Login", "Auth");
                }

                // Fetch user details
                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    _logger.LogWarning("Profile: User {UserId} not found in database", userId.Value);
                    return RedirectToAction("Login", "Auth");
                }

                // Fetch organizer's events
                var organizerEvents = await _context.Events
                    .Where(e => e.OrganizerId == userId)
                    .Include(e => e.Bookings)
                        .ThenInclude(b => b.BookingDetails)
                    .ToListAsync();

                // Calculate total tickets sold
                var totalTicketsSold = organizerEvents
                    .SelectMany(e => e.Bookings?.Where(b => b.PaymentStatus == PaymentStatus.Completed) ?? Enumerable.Empty<Booking>())
                    .SelectMany(b => b.BookingDetails ?? Enumerable.Empty<BookingDetail>())
                    .Sum(bd => bd.Quantity);

                // Calculate total revenue
                var totalRevenue = organizerEvents
                    .SelectMany(e => e.Bookings?.Where(b => b.PaymentStatus == PaymentStatus.Completed) ?? Enumerable.Empty<Booking>())
                    .Sum(b => b.FinalAmount);

                // Initialize view model
                var model = new EventOrganizerProfileViewModel
                {
                    UserId = user.UserId,
                    FirstName = user.FirstName ?? string.Empty,
                    LastName = user.LastName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    DateOfBirth = user.DateOfBirth,
                    MemberSince = user.CreatedAt ?? DateTime.UtcNow,
                    TotalEventsOrganized = organizerEvents.Count,
                    TotalTicketsSold = totalTicketsSold,
                    TotalRevenue = totalRevenue,
                    AverageRating = 0 // Placeholder for future implementation
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading organizer profile");
                TempData["ErrorMessage"] = "An error occurred while loading your profile. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: EventOrganizer/Profile
        // Updates the organizer's profile
        [HttpPost]
        [ValidateAntiForgeryToken] // Prevent cross-site request forgery
        public async Task<IActionResult> Profile(EventOrganizerProfileViewModel model)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    _logger.LogWarning("Profile POST: UserId not found in session");
                    return RedirectToAction("Login", "Auth");
                }

                // Validate model state
                if (!ModelState.IsValid)
                {
                    return View(model); // Return view with validation errors
                }

                // Additional validation
                if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
                {
                    ModelState.AddModelError(string.Empty, "First name and last name are required.");
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(model.Email) || !model.Email.Contains("@"))
                {
                    ModelState.AddModelError(string.Empty, "Valid email is required.");
                    return View(model);
                }

                // Fetch user entity
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    _logger.LogWarning("Profile POST: User {UserId} not found in database", userId.Value);
                    return RedirectToAction("Login", "Auth");
                }

                // Check if email is already taken by another user
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == model.Email && u.UserId != userId);

                if (emailExists)
                {
                    ModelState.AddModelError(string.Empty, "Email is already in use by another account.");
                    return View(model);
                }

                // Update user properties
                user.FirstName = model.FirstName.Trim();
                user.LastName = model.LastName.Trim();
                user.Email = model.Email.Trim();
                user.PhoneNumber = model.PhoneNumber?.Trim();
                user.DateOfBirth = model.DateOfBirth;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(); // Save changes to database

                _logger.LogInformation("Profile updated successfully for user {UserId}", userId.Value); // Log success
                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction(nameof(Profile));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating organizer profile");
                ModelState.AddModelError(string.Empty, "The profile was modified by another process. Please reload and try again.");
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating organizer profile");
                ModelState.AddModelError(string.Empty, "A database error occurred. Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating organizer profile");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again.");
                return View(model);
            }
        }

        // GET: EventOrganizer/MyEvents
        // Displays the organizer's events with filtering and pagination
        public async Task<IActionResult> MyEvents(string searchTerm = "", int categoryFilter = 0,
            EventStatus? statusFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    _logger.LogWarning("MyEvents: UserId not found in session");
                    return RedirectToAction("Login", "Auth");
                }

                // Validate pagination parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                // Build query for organizer's events
                var query = _context.Events
                    .Where(e => e.OrganizerId == userId)
                    .Include(e => e.Category)
                    .Include(e => e.Venue)
                    .Include(e => e.TicketCategories)
                    .Include(e => e.Bookings)
                        .ThenInclude(b => b.BookingDetails)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var search = searchTerm.Trim();
                    query = query.Where(e =>
                        (e.EventName != null && e.EventName.Contains(search)) ||
                        (e.Description != null && e.Description.Contains(search)) ||
                        (e.Venue != null && e.Venue.VenueName != null && e.Venue.VenueName.Contains(search)));
                }
                // Apply category filter
                if (categoryFilter > 0)
                {
                    query = query.Where(e => e.CategoryId == categoryFilter);
                }
                // Apply status filter
                if (statusFilter.HasValue)
                {
                    query = query.Where(e => e.Status == statusFilter.Value);
                }
                // Calculate total events and fetch paginated results
                var totalEvents = await query.CountAsync();
                var events = await query
                    .OrderByDescending(e => e.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Fetch event categories
                var categories = await _context.EventCategories.ToListAsync();

                // Initialize view model
                var viewModel = new EventOrganizerEventsViewModel
                {
                    Events = events ?? new List<Event>(),
                    Categories = categories ?? new List<EventCategory>(),
                    SearchTerm = searchTerm,
                    CategoryFilter = categoryFilter,
                    StatusFilter = statusFilter,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalEvents = totalEvents,
                    TotalPages = (int)Math.Ceiling((double)totalEvents / pageSize)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading organizer events");
                TempData["ErrorMessage"] = "An error occurred while loading your events. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: EventOrganizer/CreateEvent
        // Displays the form for creating a new event
        public async Task<IActionResult> CreateEvent()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    _logger.LogWarning("CreateEvent GET: UserId not found in session");
                    return RedirectToAction("Login", "Auth");
                }

                var categories = await _context.EventCategories
                    .Where(c => c.CategoryName != null)
                    .ToListAsync();

                var venues = await _context.Venues
                    .Where(v => v.IsActive)
                    .ToListAsync();

                if (!venues.Any())
                {
                    TempData["ErrorMessage"] = "No active venues available. Please contact administrator.";
                    return RedirectToAction(nameof(MyEvents));
                }

                var viewModel = new CreateEventOrganizerViewModel
                {
                    Categories = categories ?? new List<EventCategory>(),
                    Venues = venues,
                    EventDate = DateTime.Now.AddDays(7),
                    EndDate = DateTime.Now.AddDays(7).AddHours(3),
                    OrganizerId = userId.Value
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create event page");
                TempData["ErrorMessage"] = "An error occurred while loading the page. Please try again.";
                return RedirectToAction(nameof(MyEvents));
            }
        }

        // POST: EventOrganizer/CreateEvent
        // Handles submission of the create event form
        [HttpPost]
        [ValidateAntiForgeryToken]// Prevent cross-site request forgery
        public async Task<IActionResult> CreateEvent(CreateEventOrganizerViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                _logger.LogWarning("CreateEvent POST: UserId not found in session");
                return RedirectToAction("Login", "Auth");
            }

            // Ensure organizer can only create events for themselves
            model.OrganizerId = userId.Value;

            // Additional validation
            if (model.EventDate < DateTime.UtcNow.AddHours(1))
            {
                ModelState.AddModelError(nameof(model.EventDate), "Event date must be in the future.");
            }

            if (model.EndDate <= model.EventDate)
            {
                ModelState.AddModelError(nameof(model.EndDate), "End date must be after event start date.");
            }

            if (model.TicketCategories != null && model.TicketCategories.Any())
            {
                foreach (var tc in model.TicketCategories.Where(t => !string.IsNullOrWhiteSpace(t.CategoryName)))
                {
                    if (tc.Price < 0)
                    {
                        ModelState.AddModelError(string.Empty, $"Ticket price for '{tc.CategoryName}' cannot be negative.");
                    }
                    if (tc.TotalQuantity <= 0)
                    {
                        ModelState.AddModelError(string.Empty, $"Ticket quantity for '{tc.CategoryName}' must be greater than zero.");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();// Start database transaction
                try
                {
                    // Verify venue exists and is active
                    var venueExists = await _context.Venues
                        .AnyAsync(v => v.VenueId == model.VenueId && v.IsActive);

                    if (!venueExists)
                    {
                        ModelState.AddModelError(string.Empty, "Selected venue is not available.");
                        throw new InvalidOperationException("Invalid venue");
                    }
                    // Create new event entity
                    var eventEntity = new Event
                    {
                        EventName = model.EventName?.Trim() ?? "Unnamed Event",
                        Description = model.Description?.Trim(),
                        EventDate = model.EventDate,
                        EndDate = model.EndDate,
                        VenueId = model.VenueId,
                        OrganizerId = model.OrganizerId,
                        CategoryId = model.CategoryId,
                        BandName = model.BandName?.Trim(),
                        Performer = model.Performer?.Trim(),
                        ImageUrl = model.ImageUrl?.Trim(),
                        Status = EventStatus.Draft,
                        IsActive = model.IsActive,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Events.Add(eventEntity);
                    await _context.SaveChangesAsync();

                    // Create ticket categories if provided
                    if (model.TicketCategories != null && model.TicketCategories.Any())
                    {
                        foreach (var ticketCat in model.TicketCategories.Where(tc => !string.IsNullOrWhiteSpace(tc.CategoryName)))
                        {
                            var ticketCategory = new TicketCategory
                            {
                                EventId = eventEntity.EventId,
                                CategoryName = ticketCat.CategoryName.Trim(),
                                Price = ticketCat.Price,
                                TotalQuantity = ticketCat.TotalQuantity,
                                AvailableQuantity = ticketCat.TotalQuantity,
                                Description = ticketCat.Description?.Trim(),
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };

                            _context.TicketCategories.Add(ticketCategory);
                        }
                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();

                    _logger.LogInformation("Event {EventId} created successfully by user {UserId}",
                        eventEntity.EventId, userId.Value);
                    TempData["SuccessMessage"] = "Event created successfully! It will be reviewed by administrators before publication.";
                    return RedirectToAction(nameof(MyEvents));
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Database error creating event");
                    ModelState.AddModelError("", "A database error occurred while creating the event. Please try again.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error creating event");
                    ModelState.AddModelError("", "An unexpected error occurred while creating the event. Please try again.");
                }
            }

            // Reload dropdown data if validation fails
            try
            {
                model.Categories = await _context.EventCategories.ToListAsync();
                model.Venues = await _context.Venues.Where(v => v.IsActive).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading form data");
                model.Categories = new List<EventCategory>();
                model.Venues = new List<Venue>();
            }

            return View(model);
        }

        // GET: EventOrganizer/EditEvent/5
        // Displays the form for editing an event
        public async Task<IActionResult> EditEvent(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    _logger.LogWarning("EditEvent GET: UserId not found in session");
                    return RedirectToAction("Login", "Auth");
                }

                var eventEntity = await _context.Events
                    .Include(e => e.TicketCategories)
                    .Include(e => e.Bookings)
                    .FirstOrDefaultAsync(e => e.EventId == id && e.OrganizerId == userId);

                if (eventEntity == null)
                {
                    _logger.LogWarning("EditEvent GET: Event {EventId} not found for user {UserId}", id, userId.Value);
                    TempData["ErrorMessage"] = "Event not found or you don't have permission to edit it.";
                    return RedirectToAction(nameof(MyEvents));
                }

                // Check if event can be edited
                var hasCompletedBookings = eventEntity.Bookings?
                    .Any(b => b.PaymentStatus == PaymentStatus.Completed) ?? false;

                if (hasCompletedBookings)
                {
                    _logger.LogWarning("EditEvent GET: Event {EventId} has completed bookings", id);
                    TempData["ErrorMessage"] = "Cannot edit events with completed bookings.";
                    return RedirectToAction(nameof(MyEvents));
                }

                var categories = await _context.EventCategories.ToListAsync();
                var venues = await _context.Venues.Where(v => v.IsActive).ToListAsync();

                var viewModel = new EditEventOrganizerViewModel
                {
                    EventId = eventEntity.EventId,
                    EventName = eventEntity.EventName ?? string.Empty,
                    Description = eventEntity.Description,
                    EventDate = eventEntity.EventDate,
                    EndDate = eventEntity.EndDate,
                    VenueId = eventEntity.VenueId,
                    CategoryId = eventEntity.CategoryId,
                    BandName = eventEntity.BandName,
                    Performer = eventEntity.Performer,
                    ImageUrl = eventEntity.ImageUrl,
                    IsActive = eventEntity.IsActive,
                    Categories = categories ?? new List<EventCategory>(),
                    Venues = venues ?? new List<Venue>(),
                    TicketCategories = eventEntity.TicketCategories?.Select(tc => new TicketCategoryViewModel
                    {
                        TicketCategoryId = tc.TicketCategoryId,
                        CategoryName = tc.CategoryName ?? string.Empty,
                        Price = tc.Price,
                        TotalQuantity = tc.TotalQuantity,
                        AvailableQuantity = tc.AvailableQuantity,
                        Description = tc.Description,
                        IsActive = tc.IsActive
                    }).ToList() ?? new List<TicketCategoryViewModel>()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit event page for event {EventId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the event. Please try again.";
                return RedirectToAction(nameof(MyEvents));
            }
        }

        // POST: EventOrganizer/EditEvent/5
        // Handles submission of the edit event form
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(EditEventOrganizerViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                _logger.LogWarning("EditEvent POST: UserId not found in session");
                return RedirectToAction("Login", "Auth");
            }

            // Additional validation
            if (model.EventDate < DateTime.UtcNow.AddHours(1))
            {
                ModelState.AddModelError(nameof(model.EventDate), "Event date must be in the future.");
            }

            if (model.EndDate <= model.EventDate)
            {
                ModelState.AddModelError(nameof(model.EndDate), "End date must be after event start date.");
            }

            if (model.TicketCategories != null && model.TicketCategories.Any())
            {
                foreach (var tc in model.TicketCategories.Where(t => !string.IsNullOrWhiteSpace(t.CategoryName)))
                {
                    if (tc.Price < 0)
                    {
                        ModelState.AddModelError(string.Empty, $"Ticket price for '{tc.CategoryName}' cannot be negative.");
                    }
                    if (tc.TotalQuantity <= 0)
                    {
                        ModelState.AddModelError(string.Empty, $"Ticket quantity for '{tc.CategoryName}' must be greater than zero.");
                    }
                    if (tc.AvailableQuantity > tc.TotalQuantity)
                    {
                        ModelState.AddModelError(string.Empty, $"Available quantity for '{tc.CategoryName}' cannot exceed total quantity.");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var eventEntity = await _context.Events
                        .Include(e => e.TicketCategories)
                        .Include(e => e.Bookings)
                        .FirstOrDefaultAsync(e => e.EventId == model.EventId && e.OrganizerId == userId);

                    if (eventEntity == null)
                    {
                        _logger.LogWarning("EditEvent POST: Event {EventId} not found for user {UserId}",
                            model.EventId, userId.Value);
                        TempData["ErrorMessage"] = "Event not found or you don't have permission to edit it.";
                        return RedirectToAction(nameof(MyEvents));
                    }

                    // Check if event has completed bookings
                    var hasCompletedBookings = eventEntity.Bookings?
                        .Any(b => b.PaymentStatus == PaymentStatus.Completed) ?? false;

                    if (hasCompletedBookings)
                    {
                        _logger.LogWarning("EditEvent POST: Event {EventId} has completed bookings", model.EventId);
                        TempData["ErrorMessage"] = "Cannot edit events with completed bookings.";
                        return RedirectToAction(nameof(MyEvents));
                    }

                    // Verify venue exists and is active
                    var venueExists = await _context.Venues
                        .AnyAsync(v => v.VenueId == model.VenueId && v.IsActive);

                    if (!venueExists)
                    {
                        ModelState.AddModelError(string.Empty, "Selected venue is not available.");
                        throw new InvalidOperationException("Invalid venue");
                    }

                    // Update event properties
                    eventEntity.EventName = model.EventName?.Trim() ?? "Unnamed Event";
                    eventEntity.Description = model.Description?.Trim();
                    eventEntity.EventDate = model.EventDate;
                    eventEntity.EndDate = model.EndDate;
                    eventEntity.VenueId = model.VenueId;
                    eventEntity.CategoryId = model.CategoryId;
                    eventEntity.BandName = model.BandName?.Trim();
                    eventEntity.Performer = model.Performer?.Trim();
                    eventEntity.ImageUrl = model.ImageUrl?.Trim();
                    eventEntity.IsActive = model.IsActive;
                    eventEntity.UpdatedAt = DateTime.UtcNow;

                    // Handle ticket categories - only delete categories without any bookings
                    if (model.TicketCategories != null)
                    {
                        var existingCategories = eventEntity.TicketCategories?.ToList() ?? new List<TicketCategory>();

                        // Find categories that have booking details
                        var categoriesWithBookings = new List<int>();
                        foreach (var cat in existingCategories)
                        {
                            var hasBookingDetails = await _context.BookingDetails
                                .AnyAsync(bd => bd.TicketCategoryId == cat.TicketCategoryId);
                            if (hasBookingDetails)
                            {
                                categoriesWithBookings.Add(cat.TicketCategoryId);
                            }
                        }

                        // Remove only categories without bookings
                        var categoriesToRemove = existingCategories
                            .Where(c => !categoriesWithBookings.Contains(c.TicketCategoryId))
                            .ToList();

                        if (categoriesToRemove.Any())
                        {
                            _context.TicketCategories.RemoveRange(categoriesToRemove);
                        }

                        // Add new ticket categories
                        foreach (var ticketCat in model.TicketCategories.Where(tc => !string.IsNullOrWhiteSpace(tc.CategoryName)))
                        {
                            // Skip if this is an existing category with bookings
                            if (ticketCat.TicketCategoryId > 0 && categoriesWithBookings.Contains(ticketCat.TicketCategoryId))
                            {
                                // Update existing category (limited fields to preserve integrity)
                                var existingCat = existingCategories
                                    .FirstOrDefault(c => c.TicketCategoryId == ticketCat.TicketCategoryId);
                                if (existingCat != null)
                                {
                                    existingCat.Description = ticketCat.Description?.Trim();
                                    existingCat.IsActive = ticketCat.IsActive;
                                    // Don't modify price, quantity for categories with bookings
                                }
                                continue;
                            }

                            var ticketCategory = new TicketCategory
                            {
                                EventId = eventEntity.EventId,
                                CategoryName = ticketCat.CategoryName.Trim(),
                                Price = ticketCat.Price,
                                TotalQuantity = ticketCat.TotalQuantity,
                                AvailableQuantity = ticketCat.AvailableQuantity,
                                Description = ticketCat.Description?.Trim(),
                                IsActive = ticketCat.IsActive,
                                CreatedAt = DateTime.UtcNow
                            };

                            _context.TicketCategories.Add(ticketCategory);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Event {EventId} updated successfully by user {UserId}",
                        model.EventId, userId.Value);
                    TempData["SuccessMessage"] = "Event updated successfully!";
                    return RedirectToAction(nameof(MyEvents));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Concurrency error updating event {EventId}", model.EventId);
                    ModelState.AddModelError("", "The event was modified by another process. Please reload and try again.");
                }
                catch (DbUpdateException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Database error updating event {EventId}", model.EventId);
                    ModelState.AddModelError("", "A database error occurred while updating the event. Please try again.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error updating event {EventId}", model.EventId);
                    ModelState.AddModelError("", "An unexpected error occurred while updating the event. Please try again.");
                }
            }

            // Reload dropdown data if validation fails
            try
            {
                model.Categories = await _context.EventCategories.ToListAsync();
                model.Venues = await _context.Venues.Where(v => v.IsActive).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading form data");
                model.Categories = new List<EventCategory>();
                model.Venues = new List<Venue>();
            }

            return View(model);
        }

        // GET: EventOrganizer/EventDetails/5
        // Displays details for a specific event
        public async Task<IActionResult> EventDetails(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    _logger.LogWarning("EventDetails: UserId not found in session");
                    return RedirectToAction("Login", "Auth");
                }

                var eventEntity = await _context.Events
                    .Include(e => e.Category)
                    .Include(e => e.Venue)
                    .Include(e => e.TicketCategories)
                    .Include(e => e.Bookings)
                        .ThenInclude(b => b.BookingDetails)
                    .FirstOrDefaultAsync(e => e.EventId == id && e.OrganizerId == userId);

                if (eventEntity == null)
                {
                    _logger.LogWarning("EventDetails: Event {EventId} not found for user {UserId}", id, userId.Value);
                    TempData["ErrorMessage"] = "Event not found or you don't have permission to view it.";
                    return RedirectToAction(nameof(MyEvents));
                }

                // Calculate event statistics
                var completedBookings = eventEntity.Bookings?
                    .Where(b => b.PaymentStatus == PaymentStatus.Completed)
                    .ToList() ?? new List<Booking>();

                var totalTicketsSold = completedBookings
                    .SelectMany(b => b.BookingDetails ?? Enumerable.Empty<BookingDetail>())
                    .Sum(bd => bd.Quantity);

                var totalRevenue = completedBookings.Sum(b => b.FinalAmount);

                var totalCapacity = eventEntity.TicketCategories?.Sum(tc => tc.TotalQuantity) ?? 0;

                var viewModel = new EventOrganizerDetailsViewModel
                {
                    Event = eventEntity,
                    TotalTicketsSold = totalTicketsSold,
                    TotalRevenue = totalRevenue,
                    TotalCapacity = totalCapacity,
                    TicketsSoldPercentage = totalCapacity > 0 ? (double)totalTicketsSold / totalCapacity * 100 : 0
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading event details for event {EventId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading event details. Please try again.";
                return RedirectToAction(nameof(MyEvents));
            }
        }

        // POST: EventOrganizer/SubmitForApproval/5
        // Submits an event for admin approval
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitForApproval(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                _logger.LogWarning("SubmitForApproval: User not found in session");
                return Json(new { success = false, message = "User not found." });
            }

            try
            {
                var eventEntity = await _context.Events
                    .Include(e => e.TicketCategories)
                    .FirstOrDefaultAsync(e => e.EventId == id && e.OrganizerId == userId);

                if (eventEntity == null)
                {
                    _logger.LogWarning("SubmitForApproval: Event {EventId} not found for user {UserId}",
                        id, userId.Value);
                    return Json(new { success = false, message = "Event not found or permission denied." });
                }

                if (eventEntity.Status != EventStatus.Draft)
                {
                    return Json(new { success = false, message = "Only draft events can be submitted for approval." });
                }

                // Validate event is ready for submission
                if (string.IsNullOrWhiteSpace(eventEntity.EventName))
                {
                    return Json(new { success = false, message = "Event must have a name." });
                }

                if (eventEntity.EventDate < DateTime.UtcNow)
                {
                    return Json(new { success = false, message = "Event date must be in the future." });
                }

                if (eventEntity.TicketCategories == null || !eventEntity.TicketCategories.Any())
                {
                    return Json(new { success = false, message = "Event must have at least one ticket category." });
                }

                eventEntity.Status = EventStatus.Published; // Or EventStatus.PendingApproval if you have that status
                eventEntity.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Event {EventId} submitted for approval by user {UserId}",
                    id, userId.Value);
                return Json(new { success = true, message = "Event submitted for approval successfully!" });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error submitting event {EventId} for approval", id);
                return Json(new { success = false, message = "A database error occurred. Please try again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting event {EventId} for approval", id);
                return Json(new { success = false, message = "An unexpected error occurred while submitting the event." });
            }
        }

        // POST: EventOrganizer/DeleteEvent/5
        // Deletes an event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                _logger.LogWarning("DeleteEvent: User not found in session");
                return Json(new { success = false, message = "User not found." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("DeleteEvent: Attempting to delete event ID {EventId} for user ID {UserId}",
                    id, userId.Value);

                var eventEntity = await _context.Events
                    .Include(e => e.Bookings)
                    .Include(e => e.TicketCategories)
                    .FirstOrDefaultAsync(e => e.EventId == id && e.OrganizerId == userId);

                if (eventEntity == null)
                {
                    _logger.LogWarning("DeleteEvent: Event ID {EventId} not found or permission denied for user ID {UserId}",
                        id, userId.Value);
                    return Json(new { success = false, message = "Event not found or permission denied." });
                }

                // Check if event has any bookings (including pending)
                if (eventEntity.Bookings?.Any() == true)
                {
                    _logger.LogWarning("DeleteEvent: Event ID {EventId} has {BookingCount} bookings and cannot be deleted",
                        id, eventEntity.Bookings.Count);
                    return Json(new { success = false, message = "Cannot delete event with existing bookings." });
                }

                // Check if any ticket categories have booking details (defensive check)
                if (eventEntity.TicketCategories?.Any() == true)
                {
                    foreach (var category in eventEntity.TicketCategories)
                    {
                        var hasBookingDetails = await _context.BookingDetails
                            .AnyAsync(bd => bd.TicketCategoryId == category.TicketCategoryId);

                        if (hasBookingDetails)
                        {
                            _logger.LogWarning("DeleteEvent: Event ID {EventId} has booking details and cannot be deleted", id);
                            return Json(new { success = false, message = "Cannot delete event with existing ticket sales." });
                        }
                    }

                    _logger.LogInformation("DeleteEvent: Removing {Count} ticket categories for event ID {EventId}",
                        eventEntity.TicketCategories.Count, id);
                    _context.TicketCategories.RemoveRange(eventEntity.TicketCategories);
                }

                _logger.LogInformation("DeleteEvent: Removing event ID {EventId}", id);
                _context.Events.Remove(eventEntity);

                int rowsAffected = await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("DeleteEvent: Successfully deleted event ID {EventId}. Rows affected: {RowsAffected}",
                    id, rowsAffected);

                return Json(new { success = true, message = "Event deleted successfully." });
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Database error deleting event ID {EventId}", id);
                return Json(new { success = false, message = "A database error occurred while deleting the event. It may have related records." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting event ID {EventId}", id);
                return Json(new { success = false, message = "An unexpected error occurred while deleting the event: " + ex.Message });
            }
        }

        // GET: EventOrganizer/Logout
        // Logs out the organizer
        [HttpGet]
        [Route("EventOrganizer/Logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                HttpContext.Session.Clear();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _logger.LogInformation("User logged out successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
            }

            return RedirectToAction("Login", "Auth");
        }
    }
}