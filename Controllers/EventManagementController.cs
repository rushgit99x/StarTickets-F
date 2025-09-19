using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Filters;
using StarTickets.Models;
using StarTickets.Models.ViewModels;

namespace StarTickets.Controllers
{
    [RoleAuthorize("1")] // Admin only
    public class EventManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EventManagementController> _logger;

        public EventManagementController(ApplicationDbContext context, ILogger<EventManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: EventManagement
        public async Task<IActionResult> Index(string searchTerm = "", int categoryFilter = 0,
            EventStatus? statusFilter = null, int page = 1, int pageSize = 10)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var query = _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Include(e => e.TicketCategories)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(e => e.EventName.Contains(searchTerm) ||
                                        e.Description!.Contains(searchTerm) ||
                                        e.Venue!.VenueName.Contains(searchTerm));
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

            var totalEvents = await query.CountAsync();
            var events = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var categories = await _context.EventCategories.ToListAsync();

            var viewModel = new EventManagementViewModel
            {
                Events = events,
                Categories = categories,
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

        // GET: EventManagement/Create
        public async Task<IActionResult> Create()
        {
            var categories = await _context.EventCategories.Where(c => c.CategoryName != null).ToListAsync();
            var venues = await _context.Venues.Where(v => v.IsActive).ToListAsync();
            var organizers = await _context.Users
                .Where(u => u.Role == RoleConstants.EventOrganizerId && u.IsActive)
                .ToListAsync();

            var viewModel = new CreateEventViewModel
            {
                Categories = categories,
                Venues = venues,
                Organizers = organizers,
                EventDate = DateTime.Now.AddDays(7),
                EndDate = DateTime.Now.AddDays(7).AddHours(3)
            };

            return View(viewModel);
        }

        // POST: EventManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEventViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var eventEntity = new Event
                    {
                        EventName = model.EventName,
                        Description = model.Description,
                        EventDate = model.EventDate,
                        EndDate = model.EndDate,
                        VenueId = model.VenueId,
                        OrganizerId = model.OrganizerId,
                        CategoryId = model.CategoryId,
                        BandName = model.BandName,
                        Performer = model.Performer,
                        ImageUrl = model.ImageUrl,
                        Status = EventStatus.Published, // Admin can directly publish
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
                                CategoryName = ticketCat.CategoryName,
                                Price = ticketCat.Price,
                                TotalQuantity = ticketCat.TotalQuantity,
                                AvailableQuantity = ticketCat.TotalQuantity,
                                Description = ticketCat.Description,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            };

                            _context.TicketCategories.Add(ticketCategory);
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = "Event created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating event");
                    ModelState.AddModelError("", "An error occurred while creating the event. Please try again.");
                }
            }

            // Reload dropdown data if validation fails
            model.Categories = await _context.EventCategories.ToListAsync();
            model.Venues = await _context.Venues.Where(v => v.IsActive).ToListAsync();
            model.Organizers = await _context.Users
                .Where(u => u.Role == RoleConstants.EventOrganizerId && u.IsActive)
                .ToListAsync();

            return View(model);
        }

        // GET: EventManagement/EditForm/5
        public async Task<IActionResult> EditForm(int id)
        {
            var eventEntity = await _context.Events
                .Include(e => e.TicketCategories)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventEntity == null)
            {
                return NotFound();
            }

            var categories = await _context.EventCategories.ToListAsync();
            var venues = await _context.Venues.Where(v => v.IsActive).ToListAsync();
            var organizers = await _context.Users
                .Where(u => u.Role == RoleConstants.EventOrganizerId && u.IsActive)
                .ToListAsync();

            var viewModel = new EditEventViewModel
            {
                EventId = eventEntity.EventId,
                EventName = eventEntity.EventName,
                Description = eventEntity.Description,
                EventDate = eventEntity.EventDate,
                EndDate = eventEntity.EndDate,
                VenueId = eventEntity.VenueId,
                OrganizerId = eventEntity.OrganizerId,
                CategoryId = eventEntity.CategoryId,
                BandName = eventEntity.BandName,
                Performer = eventEntity.Performer,
                ImageUrl = eventEntity.ImageUrl,
                Status = eventEntity.Status,
                IsActive = eventEntity.IsActive,
                Categories = categories,
                Venues = venues,
                Organizers = organizers,
                TicketCategories = eventEntity.TicketCategories?.Select(tc => new TicketCategoryViewModel
                {
                    TicketCategoryId = tc.TicketCategoryId,
                    CategoryName = tc.CategoryName,
                    Price = tc.Price,
                    TotalQuantity = tc.TotalQuantity,
                    AvailableQuantity = tc.AvailableQuantity,
                    Description = tc.Description,
                    IsActive = tc.IsActive
                }).ToList() ?? new List<TicketCategoryViewModel>()
            };

            return View(viewModel);
        }

        // POST: EventManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditEventViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var eventEntity = await _context.Events
                        .Include(e => e.TicketCategories)
                        .FirstOrDefaultAsync(e => e.EventId == model.EventId);

                    if (eventEntity == null)
                    {
                        return NotFound();
                    }

                    // Update event properties
                    eventEntity.EventName = model.EventName;
                    eventEntity.Description = model.Description;
                    eventEntity.EventDate = model.EventDate;
                    eventEntity.EndDate = model.EndDate;
                    eventEntity.VenueId = model.VenueId;
                    eventEntity.OrganizerId = model.OrganizerId;
                    eventEntity.CategoryId = model.CategoryId;
                    eventEntity.BandName = model.BandName;
                    eventEntity.Performer = model.Performer;
                    eventEntity.ImageUrl = model.ImageUrl;
                    eventEntity.Status = model.Status;
                    eventEntity.IsActive = model.IsActive;
                    eventEntity.UpdatedAt = DateTime.UtcNow;

                    // Update ticket categories
                    if (model.TicketCategories != null)
                    {
                        // Remove existing ticket categories
                        var existingCategories = eventEntity.TicketCategories?.ToList() ?? new List<TicketCategory>();
                        _context.TicketCategories.RemoveRange(existingCategories);

                        // Add updated ticket categories
                        foreach (var ticketCat in model.TicketCategories.Where(tc => !string.IsNullOrWhiteSpace(tc.CategoryName)))
                        {
                            var ticketCategory = new TicketCategory
                            {
                                EventId = eventEntity.EventId,
                                CategoryName = ticketCat.CategoryName,
                                Price = ticketCat.Price,
                                TotalQuantity = ticketCat.TotalQuantity,
                                AvailableQuantity = ticketCat.AvailableQuantity,
                                Description = ticketCat.Description,
                                IsActive = ticketCat.IsActive,
                                CreatedAt = DateTime.UtcNow
                            };

                            _context.TicketCategories.Add(ticketCategory);
                        }
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Event updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating event");
                    ModelState.AddModelError("", "An error occurred while updating the event. Please try again.");
                }
            }

            // Reload dropdown data if validation fails
            model.Categories = await _context.EventCategories.ToListAsync();
            model.Venues = await _context.Venues.Where(v => v.IsActive).ToListAsync();
            model.Organizers = await _context.Users
                .Where(u => u.Role == RoleConstants.EventOrganizerId && u.IsActive)
                .ToListAsync();

            return View("EditForm", model);
        }

        // GET: EventManagement/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var eventEntity = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Include(e => e.TicketCategories)
                .Include(e => e.Bookings)
                    .ThenInclude(b => b.BookingDetails)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventEntity == null)
            {
                return NotFound();
            }

            // Calculate event statistics
            var totalTicketsSold = eventEntity.Bookings?
                .Where(b => b.PaymentStatus == PaymentStatus.Completed)
                .SelectMany(b => b.BookingDetails!)
                .Sum(bd => bd.Quantity) ?? 0;

            var totalRevenue = eventEntity.Bookings?
                .Where(b => b.PaymentStatus == PaymentStatus.Completed)
                .Sum(b => b.FinalAmount) ?? 0;

            var totalCapacity = eventEntity.TicketCategories?.Sum(tc => tc.TotalQuantity) ?? 0;

            var viewModel = new EventDetailsViewModel
            {
                Event = eventEntity,
                TotalTicketsSold = totalTicketsSold,
                TotalRevenue = totalRevenue,
                TotalCapacity = totalCapacity,
                TicketsSoldPercentage = totalCapacity > 0 ? (double)totalTicketsSold / totalCapacity * 100 : 0
            };

            return View(viewModel);
        }

        // POST: EventManagement/UpdateStatus/5
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, EventStatus status)
        {
            try
            {
                var eventEntity = await _context.Events.FindAsync(id);
                if (eventEntity == null)
                {
                    return Json(new { success = false, message = "Event not found." });
                }

                eventEntity.Status = status;
                eventEntity.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Event status updated to {status}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event status");
                return Json(new { success = false, message = "An error occurred while updating the event status." });
            }
        }

        // GET: EventManagement/Delete/5 - Shows confirmation page
        public async Task<IActionResult> Delete(int id)
        {
            var eventEntity = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Include(e => e.TicketCategories)
                .Include(e => e.Bookings)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventEntity == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(eventEntity);
        }

        // POST: EventManagement/Delete/5 - Actually deletes the event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var eventEntity = await _context.Events
                    .Include(e => e.Bookings)
                    .Include(e => e.TicketCategories)
                    .FirstOrDefaultAsync(e => e.EventId == id);

                if (eventEntity == null)
                {
                    TempData["ErrorMessage"] = "Event not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if event has bookings
                if (eventEntity.Bookings?.Any() == true)
                {
                    TempData["ErrorMessage"] = "Cannot delete event with existing bookings. Consider changing the status to 'Cancelled' instead.";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }

                // Remove ticket categories first
                if (eventEntity.TicketCategories?.Any() == true)
                {
                    _context.TicketCategories.RemoveRange(eventEntity.TicketCategories);
                }

                // Remove the event
                _context.Events.Remove(eventEntity);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Event deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event with ID {EventId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the event. Please try again.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }
        }


    }
}