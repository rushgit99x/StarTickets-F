using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Repositories.Interfaces;

namespace StarTickets.Repositories
{
    public class EventOrganizerRepository : IEventOrganizerRepository
    {
        private readonly ApplicationDbContext _context;

        public EventOrganizerRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<User?> GetUserByIdAsync(int userId) => _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        public async Task<List<Event>> GetOrganizerEventsAsync(int organizerId) => await _context.Events.Where(e => e.OrganizerId == organizerId).Include(e => e.TicketCategories).Include(e => e.Bookings).ThenInclude(b => b.BookingDetails).ToListAsync();
        public IQueryable<Event> QueryOrganizerEvents(int organizerId) => _context.Events.Where(e => e.OrganizerId == organizerId).Include(e => e.Category).Include(e => e.Venue).Include(e => e.TicketCategories).Include(e => e.Bookings).ThenInclude(b => b.BookingDetails).AsQueryable();
        public Task<List<EventCategory>> GetCategoriesAsync() => _context.EventCategories.Where(c => c.CategoryName != null).ToListAsync();
        public Task<List<Venue>> GetActiveVenuesAsync() => _context.Venues.Where(v => v.IsActive).ToListAsync();
        public async Task AddEventAsync(Event ev) { _context.Events.Add(ev); await _context.SaveChangesAsync(); }
        public async Task AddTicketCategoriesAsync(IEnumerable<TicketCategory> categories) { _context.TicketCategories.AddRange(categories); await _context.SaveChangesAsync(); }
        public Task<Event?> GetEventForOrganizerAsync(int organizerId, int eventId, bool includeTickets = false, bool includeBookings = false, bool includeCategoryVenue = false)
        {
            var q = _context.Events.AsQueryable();
            if (includeTickets) q = q.Include(e => e.TicketCategories);
            if (includeBookings) q = q.Include(e => e.Bookings).ThenInclude(b => b.BookingDetails);
            if (includeCategoryVenue) q = q.Include(e => e.Category).Include(e => e.Venue);
            return q.FirstOrDefaultAsync(e => e.EventId == eventId && e.OrganizerId == organizerId);
        }
        public Task<bool> HasBookingsAsync(int eventId) => _context.Bookings.AnyAsync(b => b.EventId == eventId);
        public void RemoveTicketCategories(IEnumerable<TicketCategory> ticketCategories) => _context.TicketCategories.RemoveRange(ticketCategories);
        public void RemoveEvent(Event ev) => _context.Events.Remove(ev);
        public Task SaveChangesAsync() => _context.SaveChangesAsync();
        public IQueryable<Booking> QueryBookingsInRangeForOrganizer(int organizerId, DateTime start, DateTime end) => _context.Bookings.AsNoTracking().Include(b => b.Event).Where(b => b.BookingDate >= start && b.BookingDate <= end && b.PaymentStatus == PaymentStatus.Completed && b.Event!.OrganizerId == organizerId);
    }
}


