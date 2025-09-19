using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Filters;
using StarTickets.Models;
using StarTickets.Models.ViewModels;

namespace StarTickets.Controllers
{
    [RoleAuthorize("1")] // Admin only
    public class VenueManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VenueManagementController> _logger;

        public VenueManagementController(ApplicationDbContext context, ILogger<VenueManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: VenueManagement
        public async Task<IActionResult> Index(string searchTerm = "", string cityFilter = "",
            bool? activeFilter = null, int page = 1, int pageSize = 10)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var query = _context.Venues
                .Include(v => v.Events)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(v => v.VenueName.Contains(searchTerm) ||
                                        v.Address.Contains(searchTerm) ||
                                        v.City.Contains(searchTerm) ||
                                        v.Country.Contains(searchTerm));
            }

            // Apply city filter
            if (!string.IsNullOrWhiteSpace(cityFilter))
            {
                query = query.Where(v => v.City == cityFilter);
            }

            // Apply active filter
            if (activeFilter.HasValue)
            {
                query = query.Where(v => v.IsActive == activeFilter.Value);
            }

            var totalVenues = await query.CountAsync();
            var venues = await query
                .OrderBy(v => v.VenueName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get distinct cities for filter dropdown
            var cities = await _context.Venues
                .Where(v => !string.IsNullOrEmpty(v.City))
                .Select(v => v.City)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var viewModel = new VenueManagementViewModel
            {
                Venues = venues,
                Cities = cities,
                SearchTerm = searchTerm,
                CityFilter = cityFilter,
                ActiveFilter = activeFilter,
                CurrentPage = page,
                PageSize = pageSize,
                TotalVenues = totalVenues,
                TotalPages = (int)Math.Ceiling((double)totalVenues / pageSize)
            };

            return View(viewModel);
        }

        // GET: VenueManagement/Create
        public IActionResult Create()
        {
            return View(new CreateVenueViewModel());
        }

        // POST: VenueManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateVenueViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var venue = new Venue
                    {
                        VenueName = model.VenueName,
                        Address = model.Address,
                        City = model.City,
                        State = model.State,
                        Country = model.Country,
                        PostalCode = model.PostalCode,
                        Capacity = model.Capacity,
                        Facilities = model.Facilities,
                        ContactPhone = model.ContactPhone,
                        ContactEmail = model.ContactEmail,
                        IsActive = model.IsActive,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Venues.Add(venue);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Venue created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating venue");
                    ModelState.AddModelError("", "An error occurred while creating the venue. Please try again.");
                }
            }

            return View(model);
        }

        // GET: VenueManagement/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                return NotFound();
            }

            var viewModel = new EditVenueViewModel
            {
                VenueId = venue.VenueId,
                VenueName = venue.VenueName,
                Address = venue.Address,
                City = venue.City,
                State = venue.State,
                Country = venue.Country,
                PostalCode = venue.PostalCode,
                Capacity = venue.Capacity,
                Facilities = venue.Facilities,
                ContactPhone = venue.ContactPhone,
                ContactEmail = venue.ContactEmail,
                IsActive = venue.IsActive
            };

            return View(viewModel);
        }

        // POST: VenueManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditVenueViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var venue = await _context.Venues.FindAsync(model.VenueId);
                    if (venue == null)
                    {
                        return NotFound();
                    }

                    venue.VenueName = model.VenueName;
                    venue.Address = model.Address;
                    venue.City = model.City;
                    venue.State = model.State;
                    venue.Country = model.Country;
                    venue.PostalCode = model.PostalCode;
                    venue.Capacity = model.Capacity;
                    venue.Facilities = model.Facilities;
                    venue.ContactPhone = model.ContactPhone;
                    venue.ContactEmail = model.ContactEmail;
                    venue.IsActive = model.IsActive;
                    venue.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Venue updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating venue");
                    ModelState.AddModelError("", "An error occurred while updating the venue. Please try again.");
                }
            }

            return View(model);
        }

        // GET: VenueManagement/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var venue = await _context.Venues
                .Include(v => v.Events!.Where(e => e.IsActive))
                .ThenInclude(e => e.Category)
                .FirstOrDefaultAsync(v => v.VenueId == id);

            if (venue == null)
            {
                return NotFound();
            }

            var totalEvents = venue.Events?.Count ?? 0;
            var upcomingEvents = venue.Events?.Count(e => e.EventDate > DateTime.UtcNow) ?? 0;
            var pastEvents = venue.Events?.Count(e => e.EventDate <= DateTime.UtcNow) ?? 0;

            // Calculate total revenue from events at this venue
            var totalRevenue = await _context.Bookings
                .Where(b => venue.Events!.Select(e => e.EventId).Contains(b.EventId) &&
                           b.PaymentStatus == PaymentStatus.Completed)
                .SumAsync(b => b.FinalAmount);

            var viewModel = new VenueDetailsViewModel
            {
                Venue = venue,
                TotalEvents = totalEvents,
                UpcomingEvents = upcomingEvents,
                PastEvents = pastEvents,
                TotalRevenue = totalRevenue,
                RecentEvents = venue.Events?.OrderByDescending(e => e.CreatedAt).Take(10).ToList() ?? new List<Event>()
            };

            return View(viewModel);
        }

        // GET: VenueManagement/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var venue = await _context.Venues
                .Include(v => v.Events)
                .FirstOrDefaultAsync(v => v.VenueId == id);

            if (venue == null)
            {
                return NotFound();
            }

            // Prevent deletion if venue has events
            if (venue.Events?.Any() == true)
            {
                TempData["ErrorMessage"] = "Cannot delete venue with existing events. Please remove or reassign events first.";
                return RedirectToAction(nameof(Index));
            }

            return View(venue); // show confirmation page
        }

        // POST: VenueManagement/DeleteConfirmed/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var venue = await _context.Venues
                    .FirstOrDefaultAsync(v => v.VenueId == id);

                if (venue == null)
                {
                    return NotFound();
                }

                _context.Venues.Remove(venue);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Venue deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting venue");
                TempData["ErrorMessage"] = "An error occurred while deleting the venue.";
                return RedirectToAction(nameof(Index));
            }
        }

    }
}