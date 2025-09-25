using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Repositories.Interfaces;

namespace StarTickets.Repositories
{
    public class HomeRepository : IHomeRepository
    {
        private readonly ApplicationDbContext _context;

        public HomeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Event>> GetFeaturedEventsAsync(int take)
        {
            return await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.TicketCategories)
                .Where(e => e.IsActive && e.Status == EventStatus.Published && e.EventDate > DateTime.UtcNow)
                .OrderBy(e => e.EventDate)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<Event>> GetEventsThisWeekAsync(DateTime nowUtc, DateTime endOfWeekUtc, int take)
        {
            return await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.TicketCategories)
                .Where(e => e.IsActive && e.Status == EventStatus.Published && e.EventDate >= nowUtc && e.EventDate < endOfWeekUtc)
                .OrderBy(e => e.EventDate)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<Event>> GetEventsThisMonthAsync(DateTime nowUtc, DateTime endOfThisMonthUtc, int take)
        {
            return await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.TicketCategories)
                .Where(e => e.IsActive && e.Status == EventStatus.Published && e.EventDate >= nowUtc && e.EventDate < endOfThisMonthUtc)
                .OrderBy(e => e.EventDate)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<Event>> GetEventsNextMonthAsync(DateTime startOfNextMonthUtc, DateTime endOfNextMonthUtc, int take)
        {
            return await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.TicketCategories)
                .Where(e => e.IsActive && e.Status == EventStatus.Published && e.EventDate >= startOfNextMonthUtc && e.EventDate < endOfNextMonthUtc)
                .OrderBy(e => e.EventDate)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<EventCategory>> GetCategoriesWithPublishedEventsAsync()
        {
            return await _context.EventCategories
                .Include(c => c.Events!.Where(e => e.IsActive && e.Status == EventStatus.Published))
                .ToListAsync();
        }

        public async Task<List<Venue>> GetActiveVenuesAsync()
        {
            return await _context.Venues
                .Where(v => v.IsActive)
                .OrderBy(v => v.City)
                .ThenBy(v => v.VenueName)
                .ToListAsync();
        }

        public async Task<List<Event>> SearchEventsAsync(string? query, int? categoryId, string? location, DateTime? date, int take)
        {
            var eventsQuery = _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.TicketCategories)
                .Where(e => e.IsActive && e.Status == EventStatus.Published && e.EventDate > DateTime.UtcNow);

            if (!string.IsNullOrWhiteSpace(query))
            {
                eventsQuery = eventsQuery.Where(e =>
                    e.EventName.Contains(query) ||
                    e.Description!.Contains(query) ||
                    e.BandName!.Contains(query) ||
                    e.Performer!.Contains(query));
            }

            if (categoryId.HasValue && categoryId > 0)
            {
                eventsQuery = eventsQuery.Where(e => e.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                eventsQuery = eventsQuery.Where(e =>
                    e.Venue!.City.Contains(location) ||
                    e.Venue!.VenueName.Contains(location));
            }

            if (date.HasValue)
            {
                var startDate = date.Value.Date;
                var endDate = startDate.AddDays(1);
                eventsQuery = eventsQuery.Where(e => e.EventDate >= startDate && e.EventDate < endDate);
            }

            return await eventsQuery
                .OrderBy(e => e.EventDate)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<Event>> GetEventsByCategoryAsync(int categoryId, int take)
        {
            return await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.TicketCategories)
                .Where(e => e.CategoryId == categoryId && e.IsActive && e.Status == EventStatus.Published && e.EventDate > DateTime.UtcNow)
                .OrderBy(e => e.EventDate)
                .Take(take)
                .ToListAsync();
        }
    }
}


