using StarTickets.Models;

namespace StarTickets.Repositories.Interfaces
{
    public interface IDashboardStatsRepository
    {
        Task<int> CountUsersAsync();
        Task<int> CountUsersSinceAsync(DateTime sinceUtc);
        Task<int> CountActivePublishedEventsAsync();
        Task<int> CountEventsSinceAsync(DateTime sinceUtc);
        Task<int> SumTicketsSoldAsync();
        Task<int> SumTicketsSoldSinceAsync(DateTime sinceUtc);
        Task<decimal> SumRevenueAsync();
        Task<decimal> SumRevenueSinceAsync(DateTime sinceUtc);
        Task<int> CountPendingEventsAsync();
        Task<List<object>> GetRecentActivitiesAsync(int take);
        Task<List<(string MonthLabel, decimal Revenue)>> GetRevenueByMonthAsync(int monthsBack);
        Task<List<object>> GetEventStatusDistributionAsync();
        Task<List<object>> GetCategoryDistributionAsync();
        Task<List<object>> GetTopPerformingEventsAsync(int count);
    }
}


