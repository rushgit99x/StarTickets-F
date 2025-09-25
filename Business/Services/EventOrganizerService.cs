using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StarTickets.Models;
using StarTickets.Models.ViewModels;
using StarTickets.Repositories.Interfaces;
using StarTickets.Services.Interfaces;

namespace StarTickets.Services
{
    public class EventOrganizerService : IEventOrganizerService
    {
        private readonly IEventOrganizerRepository _repo;
        private readonly IPdfReportService _pdfReportService;
        private readonly ILogger<EventOrganizerService> _logger;

        public EventOrganizerService(IEventOrganizerRepository repo, IPdfReportService pdfReportService, ILogger<EventOrganizerService> logger)
        {
            _repo = repo;
            _pdfReportService = pdfReportService;
            _logger = logger;
        }

        public async Task<EventOrganizerDashboardViewModel> GetDashboardAsync(int organizerId)
        {
            var events = await _repo.GetOrganizerEventsAsync(organizerId);
            return new EventOrganizerDashboardViewModel
            {
                TotalEvents = events.Count,
                ActiveEvents = events.Count(e => e.Status == EventStatus.Published && e.IsActive),
                TotalTicketsSold = events.SelectMany(e => e.Bookings?.Where(b => b.PaymentStatus == PaymentStatus.Completed) ?? new List<Booking>()).SelectMany(b => b.BookingDetails ?? new List<BookingDetail>()).Sum(bd => bd.Quantity),
                TotalRevenue = events.SelectMany(e => e.Bookings?.Where(b => b.PaymentStatus == PaymentStatus.Completed) ?? new List<Booking>()).Sum(b => b.FinalAmount),
                RecentEvents = events.OrderByDescending(e => e.CreatedAt).Take(5).ToList()
            };
        }

        public async Task<ReportsViewModel> GetSalesReportModelAsync(int organizerId, DateTime? startDate, DateTime? endDate, int? eventId)
        {
            DateTime start = startDate?.Date ?? DateTime.UtcNow.AddDays(-30).Date;
            DateTime end = (endDate?.Date ?? DateTime.UtcNow.Date).AddDays(1).AddTicks(-1);
            var bookingsInRange = _repo.QueryBookingsInRangeForOrganizer(organizerId, start, end);
            if (eventId.HasValue && eventId.Value > 0) bookingsInRange = bookingsInRange.Where(b => b.EventId == eventId.Value);

            var model = new ReportsViewModel { Filter = new ReportsFilterViewModel { StartDate = start, EndDate = end } };
            model.Kpis.TotalSales = await bookingsInRange.SumAsync(b => (decimal?)b.FinalAmount) ?? 0m;
            model.Kpis.TotalBookings = await bookingsInRange.CountAsync();

            var today = DateTime.UtcNow.Date; var tomorrow = today.AddDays(1).AddTicks(-1);
            var bookingsToday = _repo.QueryBookingsInRangeForOrganizer(organizerId, today, tomorrow);
            if (eventId.HasValue && eventId.Value > 0) bookingsToday = bookingsToday.Where(b => b.EventId == eventId.Value);
            model.Kpis.SalesToday = await bookingsToday.SumAsync(b => (decimal?)b.FinalAmount) ?? 0m;
            model.Kpis.BookingsToday = await bookingsToday.CountAsync();

            model.SalesByDate = await bookingsInRange.GroupBy(b => b.BookingDate.Date).Select(g => new SalesByDateItem { Date = g.Key, Amount = g.Sum(x => x.FinalAmount) }).OrderBy(x => x.Date).ToListAsync();
            model.TopEvents = await bookingsInRange.GroupBy(b => new { b.EventId, b.Event!.EventName }).Select(g => new TopEventSalesItem { EventId = g.Key.EventId, EventName = g.Key.EventName, Sales = g.Sum(x => x.FinalAmount), Bookings = g.Count() }).OrderByDescending(x => x.Sales).Take(10).ToListAsync();
            return model;
        }

        public async Task<List<SelectListItem>> GetOrganizerEventOptionsAsync(int organizerId)
        {
            return await _repo.QueryOrganizerEvents(organizerId)
                .AsNoTracking()
                .OrderByDescending(e => e.EventDate)
                .Select(e => new SelectListItem { Value = e.EventId.ToString(), Text = e.EventName })
                .ToListAsync();
        }

        public async Task<EventOrganizerProfileViewModel?> GetProfileAsync(int organizerId)
        {
            var user = await _repo.GetUserByIdAsync(organizerId);
            if (user == null) return null;
            var events = await _repo.GetOrganizerEventsAsync(organizerId);
            var totalTicketsSold = events.SelectMany(e => e.Bookings?.Where(b => b.PaymentStatus == PaymentStatus.Completed) ?? new List<Booking>()).SelectMany(b => b.BookingDetails ?? new List<BookingDetail>()).Sum(bd => bd.Quantity);
            var totalRevenue = events.SelectMany(e => e.Bookings?.Where(b => b.PaymentStatus == PaymentStatus.Completed) ?? new List<Booking>()).Sum(b => b.FinalAmount);
            return new EventOrganizerProfileViewModel
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                MemberSince = user.CreatedAt ?? DateTime.UtcNow,
                TotalEventsOrganized = events.Count,
                TotalTicketsSold = totalTicketsSold,
                TotalRevenue = totalRevenue,
                AverageRating = 0
            };
        }

        public async Task<bool> UpdateProfileAsync(int organizerId, EventOrganizerProfileViewModel model)
        {
            try
            {
                var user = await _repo.GetUserByIdAsync(organizerId);
                if (user == null) return false;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;
                user.PhoneNumber = model.PhoneNumber;
                user.DateOfBirth = model.DateOfBirth;
                user.UpdatedAt = DateTime.UtcNow;
                await _repo.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating organizer profile");
                return false;
            }
        }

        public async Task<EventOrganizerEventsViewModel> GetMyEventsAsync(int organizerId, string searchTerm, int categoryFilter, EventStatus? statusFilter, int page, int pageSize)
        {
            var query = _repo.QueryOrganizerEvents(organizerId);
            if (!string.IsNullOrWhiteSpace(searchTerm)) query = query.Where(e => e.EventName.Contains(searchTerm) || e.Description!.Contains(searchTerm) || e.Venue!.VenueName.Contains(searchTerm));
            if (categoryFilter > 0) query = query.Where(e => e.CategoryId == categoryFilter);
            if (statusFilter.HasValue) query = query.Where(e => e.Status == statusFilter.Value);
            var totalEvents = await query.CountAsync();
            var events = await query.OrderByDescending(e => e.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var categories = await _repo.GetCategoriesAsync();
            return new EventOrganizerEventsViewModel
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
        }

        public async Task<CreateEventOrganizerViewModel> GetCreateEventFormAsync(int organizerId)
        {
            return new CreateEventOrganizerViewModel
            {
                Categories = await _repo.GetCategoriesAsync(),
                Venues = await _repo.GetActiveVenuesAsync(),
                EventDate = DateTime.Now.AddDays(7),
                EndDate = DateTime.Now.AddDays(7).AddHours(3),
                OrganizerId = organizerId
            };
        }

        public async Task<(bool Success, string Message)> CreateEventAsync(int organizerId, CreateEventOrganizerViewModel model)
        {
            try
            {
                var ev = new Event
                {
                    EventName = model.EventName,
                    Description = model.Description,
                    EventDate = model.EventDate,
                    EndDate = model.EndDate,
                    VenueId = model.VenueId,
                    OrganizerId = organizerId,
                    CategoryId = model.CategoryId,
                    BandName = model.BandName,
                    Performer = model.Performer,
                    ImageUrl = model.ImageUrl,
                    Status = EventStatus.Draft,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _repo.AddEventAsync(ev);
                if (model.TicketCategories != null && model.TicketCategories.Any())
                {
                    var cats = model.TicketCategories.Where(tc => !string.IsNullOrWhiteSpace(tc.CategoryName)).Select(tc => new TicketCategory
                    {
                        EventId = ev.EventId,
                        CategoryName = tc.CategoryName,
                        Price = tc.Price,
                        TotalQuantity = tc.TotalQuantity,
                        AvailableQuantity = tc.TotalQuantity,
                        Description = tc.Description,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    await _repo.AddTicketCategoriesAsync(cats);
                }
                return (true, "Event created successfully! It will be reviewed by administrators before publication.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                return (false, "An error occurred while creating the event. Please try again.");
            }
        }

        public async Task<EditEventOrganizerViewModel?> GetEditEventAsync(int organizerId, int eventId)
        {
            var ev = await _repo.GetEventForOrganizerAsync(organizerId, eventId, includeTickets: true);
            if (ev == null) return null;
            var hasBookings = await _repo.HasBookingsAsync(eventId);
            if (ev.Status == EventStatus.Published && hasBookings) return null;
            return new EditEventOrganizerViewModel
            {
                EventId = ev.EventId,
                EventName = ev.EventName,
                Description = ev.Description,
                EventDate = ev.EventDate,
                EndDate = ev.EndDate,
                VenueId = ev.VenueId,
                CategoryId = ev.CategoryId,
                BandName = ev.BandName,
                Performer = ev.Performer,
                ImageUrl = ev.ImageUrl,
                IsActive = ev.IsActive,
                Categories = await _repo.GetCategoriesAsync(),
                Venues = await _repo.GetActiveVenuesAsync(),
                TicketCategories = ev.TicketCategories?.Select(tc => new TicketCategoryViewModel
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
        }

        public async Task<(bool Success, string Message)> EditEventAsync(int organizerId, EditEventOrganizerViewModel model)
        {
            try
            {
                var ev = await _repo.GetEventForOrganizerAsync(organizerId, model.EventId, includeTickets: true);
                if (ev == null) return (false, "Event not found or you don't have permission to edit it.");
                ev.EventName = model.EventName;
                ev.Description = model.Description;
                ev.EventDate = model.EventDate;
                ev.EndDate = model.EndDate;
                ev.VenueId = model.VenueId;
                ev.CategoryId = model.CategoryId;
                ev.BandName = model.BandName;
                ev.Performer = model.Performer;
                ev.ImageUrl = model.ImageUrl;
                ev.IsActive = model.IsActive;
                ev.UpdatedAt = DateTime.UtcNow;

                if (model.TicketCategories != null)
                {
                    var existing = ev.TicketCategories?.ToList() ?? new List<TicketCategory>();
                    _repo.RemoveTicketCategories(existing);
                    var newCats = model.TicketCategories.Where(tc => !string.IsNullOrWhiteSpace(tc.CategoryName)).Select(tc => new TicketCategory
                    {
                        EventId = ev.EventId,
                        CategoryName = tc.CategoryName,
                        Price = tc.Price,
                        TotalQuantity = tc.TotalQuantity,
                        AvailableQuantity = tc.AvailableQuantity,
                        Description = tc.Description,
                        IsActive = tc.IsActive,
                        CreatedAt = DateTime.UtcNow
                    });
                    await _repo.AddTicketCategoriesAsync(newCats);
                }

                await _repo.SaveChangesAsync();
                return (true, "Event updated successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event");
                return (false, "An error occurred while updating the event. Please try again.");
            }
        }

        public async Task<EventOrganizerDetailsViewModel?> GetEventDetailsAsync(int organizerId, int eventId)
        {
            var ev = await _repo.GetEventForOrganizerAsync(organizerId, eventId, includeBookings: true, includeCategoryVenue: true);
            if (ev == null) return null;
            var totalTicketsSold = ev.Bookings?.Where(b => b.PaymentStatus == PaymentStatus.Completed).SelectMany(b => b.BookingDetails!).Sum(bd => bd.Quantity) ?? 0;
            var totalRevenue = ev.Bookings?.Where(b => b.PaymentStatus == PaymentStatus.Completed).Sum(b => b.FinalAmount) ?? 0;
            var totalCapacity = ev.TicketCategories?.Sum(tc => tc.TotalQuantity) ?? 0;
            return new EventOrganizerDetailsViewModel
            {
                Event = ev,
                TotalTicketsSold = totalTicketsSold,
                TotalRevenue = totalRevenue,
                TotalCapacity = totalCapacity,
                TicketsSoldPercentage = totalCapacity > 0 ? (double)totalTicketsSold / totalCapacity * 100 : 0
            };
        }

        public async Task<(bool Success, string Message)> SubmitForApprovalAsync(int organizerId, int eventId)
        {
            try
            {
                var ev = await _repo.GetEventForOrganizerAsync(organizerId, eventId);
                if (ev == null) return (false, "Event not found or permission denied.");
                if (ev.Status != EventStatus.Draft) return (false, "Only draft events can be submitted for approval.");
                ev.Status = EventStatus.Published;
                ev.UpdatedAt = DateTime.UtcNow;
                await _repo.SaveChangesAsync();
                return (true, "Event submitted for approval successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting event for approval");
                return (false, "An error occurred while submitting the event.");
            }
        }

        public async Task<(bool Success, string Message)> DeleteEventAsync(int organizerId, int eventId)
        {
            try
            {
                var ev = await _repo.GetEventForOrganizerAsync(organizerId, eventId, includeTickets: true, includeBookings: true);
                if (ev == null) return (false, "Event not found or permission denied.");
                if (ev.Bookings?.Any() == true) return (false, "Cannot delete event with existing bookings.");
                if (ev.TicketCategories?.Any() == true) _repo.RemoveTicketCategories(ev.TicketCategories);
                _repo.RemoveEvent(ev);
                await _repo.SaveChangesAsync();
                return (true, "Event deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event");
                return (false, "An error occurred while deleting the event.");
            }
        }
    }
}


