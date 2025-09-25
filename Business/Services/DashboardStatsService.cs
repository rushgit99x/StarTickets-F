using StarTickets.Repositories.Interfaces;
using StarTickets.Services.Interfaces;

namespace StarTickets.Services
{
    public class DashboardStatsService : IDashboardStatsService
    {
        private readonly IDashboardStatsRepository _repo;

        public DashboardStatsService(IDashboardStatsRepository repo)
        {
            _repo = repo;
        }

        public async Task<object> GetStatsAsync()
        {
            var now = DateTime.UtcNow;
            var lastMonth = now.AddMonths(-1);

            var totalUsers = await _repo.CountUsersAsync();
            var usersLastMonth = await _repo.CountUsersSinceAsync(lastMonth);
            var userGrowthPercent = totalUsers > 0 ? Math.Round(((double)usersLastMonth / totalUsers) * 100, 1) : 0;

            var activeEvents = await _repo.CountActivePublishedEventsAsync();
            var eventsThisWeek = await _repo.CountEventsSinceAsync(now.AddDays(-7));

            var totalTicketsSold = await _repo.SumTicketsSoldAsync();
            var ticketsSoldLastMonth = await _repo.SumTicketsSoldSinceAsync(lastMonth);
            var ticketGrowthPercent = totalTicketsSold > 0 ? Math.Round(((double)ticketsSoldLastMonth / totalTicketsSold) * 100, 1) : 0;

            var totalRevenue = await _repo.SumRevenueAsync();
            var revenueLastMonth = await _repo.SumRevenueSinceAsync(lastMonth);
            var revenueGrowthPercent = totalRevenue > 0 ? Math.Round(((double)revenueLastMonth / (double)totalRevenue) * 100, 1) : 0;

            var pendingEvents = await _repo.CountPendingEventsAsync();

            var recentActivitiesRaw = await _repo.GetRecentActivitiesAsync(10);
            var recentActivities = recentActivitiesRaw.Select(x =>
            {
                dynamic d = x;
                return new
                {
                    Action = d.Action,
                    UserName = d.UserName,
                    Details = d.Details,
                    CreatedAt = d.CreatedAt,
                    TimeAgo = GetTimeAgo(d.CreatedAt)
                } as object;
            }).ToList();

            var revenueChart = await _repo.GetRevenueByMonthAsync(12);
            var revenueChartData = revenueChart.Select(r => new { Month = r.MonthLabel, Revenue = r.Revenue } as object).ToList();

            return new
            {
                success = true,
                stats = new
                {
                    TotalUsers = totalUsers,
                    UserGrowthPercent = userGrowthPercent,
                    ActiveEvents = activeEvents,
                    EventsThisWeek = eventsThisWeek,
                    TotalTicketsSold = totalTicketsSold,
                    TicketGrowthPercent = ticketGrowthPercent,
                    TotalRevenue = totalRevenue,
                    RevenueGrowthPercent = revenueGrowthPercent,
                    PendingEvents = pendingEvents
                },
                RecentActivities = recentActivities,
                RevenueChartData = revenueChartData
            };
        }

        public Task<List<object>> GetEventStatusDistributionAsync() => _repo.GetEventStatusDistributionAsync();
        public Task<List<object>> GetCategoryDistributionAsync() => _repo.GetCategoryDistributionAsync();
        public Task<List<object>> GetTopPerformingEventsAsync(int count) => _repo.GetTopPerformingEventsAsync(count);

        private string GetTimeAgo(DateTime? dateTime)
        {
            if (!dateTime.HasValue) return "Unknown";
            var timeSpan = DateTime.UtcNow - dateTime.Value;
            if (timeSpan.Days > 0) return $"{timeSpan.Days} day{(timeSpan.Days > 1 ? "s" : "")} ago";
            if (timeSpan.Hours > 0) return $"{timeSpan.Hours} hour{(timeSpan.Hours > 1 ? "s" : "")} ago";
            if (timeSpan.Minutes > 0) return $"{timeSpan.Minutes} minute{(timeSpan.Minutes > 1 ? "s" : "")} ago";
            return "Just now";
        }
    }
}


