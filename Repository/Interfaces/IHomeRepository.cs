using StarTickets.Models;

namespace StarTickets.Repositories.Interfaces
{
    public interface IHomeRepository
    {
        Task<List<Event>> GetFeaturedEventsAsync(int take);
        Task<List<Event>> GetEventsThisWeekAsync(DateTime nowUtc, DateTime endOfWeekUtc, int take);
        Task<List<Event>> GetEventsThisMonthAsync(DateTime nowUtc, DateTime endOfThisMonthUtc, int take);
        Task<List<Event>> GetEventsNextMonthAsync(DateTime startOfNextMonthUtc, DateTime endOfNextMonthUtc, int take);

        Task<List<EventCategory>> GetCategoriesWithPublishedEventsAsync();
        Task<List<Venue>> GetActiveVenuesAsync();

        Task<List<Event>> SearchEventsAsync(string? query, int? categoryId, string? location, DateTime? date, int take);
        Task<List<Event>> GetEventsByCategoryAsync(int categoryId, int take);
    }
}


