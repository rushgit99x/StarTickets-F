using StarTickets.Models;

namespace StarTickets.Repositories.Interfaces
{
    public interface IReportsRepository
    {
        IQueryable<Booking> QueryCompletedBookingsInRange(DateTime start, DateTime end);
        Task<int> CountEventsAsync();
        Task<int> CountUsersAsync();
        Task<int> CountUpcomingEventsAsync(DateTime today);
        Task<int> CountNewUsersSinceAsync(DateTime monthStart);
    }
}


