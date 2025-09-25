using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Repositories.Interfaces;

namespace StarTickets.Repositories
{
    public class EventManagementRepository : IEventManagementRepository
    {
        private readonly ApplicationDbContext _context;

        public EventManagementRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<Event> QueryEvents()
        {
            return _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Include(e => e.TicketCategories)
                .AsQueryable();
        }

        public async Task<List<EventCategory>> GetAllCategoriesAsync()
        {
            return await _context.EventCategories.ToListAsync();
        }

        public async Task<List<Venue>> GetActiveVenuesAsync()
        {
            return await _context.Venues.Where(v => v.IsActive).ToListAsync();
        }

        public async Task<List<User>> GetActiveOrganizersAsync()
        {
            return await _context.Users
                .Where(u => u.Role == RoleConstants.EventOrganizerId && u.IsActive)
                .ToListAsync();
        }

        public async Task AddEventAsync(Event eventEntity)
        {
            _context.Events.Add(eventEntity);
            await _context.SaveChangesAsync();
        }

        public async Task AddTicketCategoriesAsync(IEnumerable<TicketCategory> categories)
        {
            _context.TicketCategories.AddRange(categories);
            await _context.SaveChangesAsync();
        }

        public async Task<Event?> GetEventWithCategoriesAsync(int id)
        {
            return await _context.Events
                .Include(e => e.TicketCategories)
                .FirstOrDefaultAsync(e => e.EventId == id);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<Event?> GetEventWithDetailsAsync(int id)
        {
            return await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Include(e => e.TicketCategories)
                .Include(e => e.Bookings) 
                    .ThenInclude(b => b.BookingDetails)
                .FirstOrDefaultAsync(e => e.EventId == id);
        }

        public async Task<Event?> GetEventForDeletionAsync(int id)
        {
            return await _context.Events
                .Include(e => e.Bookings)
                .Include(e => e.TicketCategories)
                .FirstOrDefaultAsync(e => e.EventId == id);
        }

        public void RemoveTicketCategories(IEnumerable<TicketCategory> ticketCategories)
        {
            _context.TicketCategories.RemoveRange(ticketCategories);
        }

        public void RemoveEvent(Event eventEntity)
        {
            _context.Events.Remove(eventEntity);
        }
    }
}


