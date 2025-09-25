namespace StarTickets.Services.Interfaces
{
    public interface IDashboardStatsService
    {
        Task<object> GetStatsAsync();
        Task<List<object>> GetEventStatusDistributionAsync();
        Task<List<object>> GetCategoryDistributionAsync();
        Task<List<object>> GetTopPerformingEventsAsync(int count);
    }
}


