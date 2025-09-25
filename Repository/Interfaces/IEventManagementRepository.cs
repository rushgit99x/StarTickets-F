using StarTickets.Models;
using StarTickets.Models.ViewModels;

namespace StarTickets.Repositories.Interfaces
{
    public interface IEventManagementRepository
    {
        IQueryable<Event> QueryEvents();
        Task<List<EventCategory>> GetAllCategoriesAsync();
        Task<List<Venue>> GetActiveVenuesAsync();
        Task<List<User>> GetActiveOrganizersAsync();
        Task AddEventAsync(Event eventEntity);
        Task AddTicketCategoriesAsync(IEnumerable<TicketCategory> categories);
        Task<Event?> GetEventWithCategoriesAsync(int id);
        Task SaveChangesAsync();
        Task<Event?> GetEventWithDetailsAsync(int id);
        Task<Event?> GetEventForDeletionAsync(int id);
        void RemoveTicketCategories(IEnumerable<TicketCategory> ticketCategories);
        void RemoveEvent(Event eventEntity);
    }
}


