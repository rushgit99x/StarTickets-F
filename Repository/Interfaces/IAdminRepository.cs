using StarTickets.Models;

namespace StarTickets.Repositories.Interfaces
{
    public interface IAdminRepository
    {
        int GetTotalUsers();
        int GetActiveEvents();
        int GetTicketsSold();
        decimal GetTotalRevenue();

        User? GetUserById(int userId);

        Dictionary<int, decimal> GetMonthlyRevenueByMonth(int year);

        List<(int EventId, string EventName, string OrganizerName, DateTime EventDate, string VenueName, EventStatus Status)> GetRecentEventsBasic(int take);

        Dictionary<int, int> GetTicketsSoldByEventIds(IEnumerable<int> eventIds);
        Dictionary<int, decimal> GetRevenueByEventIds(IEnumerable<int> eventIds);
    }
}


