using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Repositories.Interfaces;

namespace StarTickets.Repositories
{
    public class DashboardStatsRepository : IDashboardStatsRepository
    {
        private readonly ApplicationDbContext _context;

        public DashboardStatsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<int> CountUsersAsync() => _context.Users.CountAsync();
        public Task<int> CountUsersSinceAsync(DateTime sinceUtc) => _context.Users.CountAsync(u => u.CreatedAt >= sinceUtc);
        public Task<int> CountActivePublishedEventsAsync() => _context.Events.CountAsync(e => e.Status == EventStatus.Published && e.IsActive);
        public Task<int> CountEventsSinceAsync(DateTime sinceUtc) => _context.Events.CountAsync(e => e.CreatedAt >= sinceUtc);
        public Task<int> SumTicketsSoldAsync() => _context.BookingDetails.Where(bd => bd.Booking!.PaymentStatus == PaymentStatus.Completed).SumAsync(bd => bd.Quantity);
        public Task<int> SumTicketsSoldSinceAsync(DateTime sinceUtc) => _context.BookingDetails.Where(bd => bd.Booking!.PaymentStatus == PaymentStatus.Completed && bd.Booking.BookingDate >= sinceUtc).SumAsync(bd => bd.Quantity);
        public async Task<decimal> SumRevenueAsync() => await _context.Bookings.Where(b => b.PaymentStatus == PaymentStatus.Completed).SumAsync(b => b.FinalAmount);
        public async Task<decimal> SumRevenueSinceAsync(DateTime sinceUtc) => await _context.Bookings.Where(b => b.PaymentStatus == PaymentStatus.Completed && b.BookingDate >= sinceUtc).SumAsync(b => b.FinalAmount);
        public Task<int> CountPendingEventsAsync() => _context.Events.CountAsync(e => e.Status == EventStatus.Draft);

        public async Task<List<object>> GetRecentActivitiesAsync(int take)
        {
            var activities = await _context.UserActivityLogs
                .Include(log => log.User)
                .OrderByDescending(log => log.CreatedAt)
                .Take(take)
                .Select(log => new
                {
                    Action = log.Action,
                    UserName = log.User != null ? (log.User.FirstName + " " + log.User.LastName) : "System",
                    Details = log.Details,
                    CreatedAt = log.CreatedAt
                })
                .ToListAsync();

            return activities.Cast<object>().ToList();
        }

        public async Task<List<(string MonthLabel, decimal Revenue)>> GetRevenueByMonthAsync(int monthsBack)
        {
            var now = DateTime.UtcNow;
            var results = new List<(string, decimal)>();
            for (int i = monthsBack - 1; i >= 0; i--)
            {
                var monthStart = new DateTime(now.AddMonths(-i).Year, now.AddMonths(-i).Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                var revenue = await _context.Bookings
                    .Where(b => b.PaymentStatus == PaymentStatus.Completed && b.BookingDate >= monthStart && b.BookingDate <= monthEnd)
                    .SumAsync(b => b.FinalAmount);
                results.Add((monthStart.ToString("MMM yyyy"), revenue));
            }
            return results;
        }

        public async Task<List<object>> GetEventStatusDistributionAsync()
        {
            var distribution = await _context.Events
                .GroupBy(e => e.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            return distribution.Cast<object>().ToList();
        }

        public async Task<List<object>> GetCategoryDistributionAsync()
        {
            var distribution = await _context.Events
                .Include(e => e.Category)
                .Where(e => e.Status == EventStatus.Published)
                .GroupBy(e => e.Category!.CategoryName)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            return distribution.Cast<object>().ToList();
        }

        public async Task<List<object>> GetTopPerformingEventsAsync(int count)
        {
            var events = await _context.Events
                .Include(e => e.Bookings)
                .Where(e => e.Status == EventStatus.Published)
                .Select(e => new
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    Revenue = e.Bookings!.Where(b => b.PaymentStatus == PaymentStatus.Completed).Sum(b => b.FinalAmount),
                    TicketsSold = e.Bookings!.Where(b => b.PaymentStatus == PaymentStatus.Completed).SelectMany(b => b.BookingDetails!).Sum(bd => bd.Quantity)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(count)
                .ToListAsync();

            return events.Cast<object>().ToList();
        }
    }
}