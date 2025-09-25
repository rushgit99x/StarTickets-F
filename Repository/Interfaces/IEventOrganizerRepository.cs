using StarTickets.Models;

namespace StarTickets.Repositories.Interfaces
{
    public interface IEventOrganizerRepository
    {
        Task<User?> GetUserByIdAsync(int userId);
        Task<List<Event>> GetOrganizerEventsAsync(int organizerId);
        IQueryable<Event> QueryOrganizerEvents(int organizerId);
        Task<List<EventCategory>> GetCategoriesAsync();
        Task<List<Venue>> GetActiveVenuesAsync();
        Task AddEventAsync(Event ev);
        Task AddTicketCategoriesAsync(IEnumerable<TicketCategory> categories);
        Task<Event?> GetEventForOrganizerAsync(int organizerId, int eventId, bool includeTickets = false, bool includeBookings = false, bool includeCategoryVenue = false);
        Task<bool> HasBookingsAsync(int eventId);
        void RemoveTicketCategories(IEnumerable<TicketCategory> ticketCategories);
        void RemoveEvent(Event ev);
        Task SaveChangesAsync();
        IQueryable<Booking> QueryBookingsInRangeForOrganizer(int organizerId, DateTime start, DateTime end);
    }
}


