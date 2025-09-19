using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Filters;
using StarTickets.Models;

namespace StarTickets.Controllers
{
    [RoleAuthorize("1")] // Admin only
    public class DashboardStatsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardStatsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: API endpoint for dashboard statistics
        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var now = DateTime.UtcNow;
                var lastMonth = now.AddMonths(-1);

                // User Statistics
                var totalUsers = await _context.Users.CountAsync();
                var usersLastMonth = await _context.Users.CountAsync(u => u.CreatedAt >= lastMonth);
                var userGrowthPercent = totalUsers > 0 ? Math.Round(((double)usersLastMonth / totalUsers) * 100, 1) : 0;

                // Event Statistics
                var activeEvents = await _context.Events.CountAsync(e => e.Status == EventStatus.Published && e.IsActive);
                var eventsThisWeek = await _context.Events.CountAsync(e => e.CreatedAt >= now.AddDays(-7));

                // Ticket Statistics
                var totalTicketsSold = await _context.BookingDetails
                    .Where(bd => bd.Booking!.PaymentStatus == PaymentStatus.Completed)
                    .SumAsync(bd => bd.Quantity);

                var ticketsSoldLastMonth = await _context.BookingDetails
                    .Where(bd => bd.Booking!.PaymentStatus == PaymentStatus.Completed &&
                                bd.Booking.BookingDate >= lastMonth)
                    .SumAsync(bd => bd.Quantity);

                var ticketGrowthPercent = totalTicketsSold > 0 ?
                    Math.Round(((double)ticketsSoldLastMonth / totalTicketsSold) * 100, 1) : 0;

                // Revenue Statistics
                var totalRevenue = await _context.Bookings
                    .Where(b => b.PaymentStatus == PaymentStatus.Completed)
                    .SumAsync(b => b.FinalAmount);

                var revenueLastMonth = await _context.Bookings
                    .Where(b => b.PaymentStatus == PaymentStatus.Completed && b.BookingDate >= lastMonth)
                    .SumAsync(b => b.FinalAmount);

                var revenueGrowthPercent = totalRevenue > 0 ?
                    Math.Round(((double)revenueLastMonth / (double)totalRevenue) * 100, 1) : 0;

                // Pending approvals
                var pendingEvents = await _context.Events.CountAsync(e => e.Status == EventStatus.Draft);

                // Recent activities
                var recentActivities = await _context.UserActivityLogs
                    .Include(log => log.User)
                    .OrderByDescending(log => log.CreatedAt)
                    .Take(10)
                    .Select(log => new
                    {
                        Action = log.Action,
                        UserName = log.User != null ? $"{log.User.FirstName} {log.User.LastName}" : "System",
                        Details = log.Details,
                        CreatedAt = log.CreatedAt,
                        TimeAgo = GetTimeAgo(log.CreatedAt)
                    })
                    .ToListAsync();

                // Revenue chart data (last 12 months)
                var revenueChartData = new List<object>();
                for (int i = 11; i >= 0; i--)
                {
                    var monthStart = now.AddMonths(-i).Date.AddDays(1 - now.AddMonths(-i).Day);
                    var monthEnd = monthStart.AddMonths(1).AddDays(-1);

                    var monthlyRevenue = await _context.Bookings
                        .Where(b => b.PaymentStatus == PaymentStatus.Completed &&
                                   b.BookingDate >= monthStart && b.BookingDate <= monthEnd)
                        .SumAsync(b => b.FinalAmount);

                    revenueChartData.Add(new
                    {
                        Month = monthStart.ToString("MMM yyyy"),
                        Revenue = monthlyRevenue
                    });
                }

                return Json(new
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
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: API endpoint for event status distribution
        [HttpGet]
        public async Task<IActionResult> GetEventStatusDistribution()
        {
            try
            {
                var statusDistribution = await _context.Events
                    .GroupBy(e => e.Status)
                    .Select(g => new
                    {
                        Status = g.Key.ToString(),
                        Count = g.Count()
                    })
                    .ToListAsync();

                return Json(new { success = true, data = statusDistribution });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: API endpoint for category-wise event distribution
        [HttpGet]
        public async Task<IActionResult> GetCategoryDistribution()
        {
            try
            {
                var categoryDistribution = await _context.Events
                    .Include(e => e.Category)
                    .Where(e => e.Status == EventStatus.Published)
                    .GroupBy(e => e.Category!.CategoryName)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                return Json(new { success = true, data = categoryDistribution });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: API endpoint for top performing events
        [HttpGet]
        public async Task<IActionResult> GetTopPerformingEvents(int count = 5)
        {
            try
            {
                var topEvents = await _context.Events
                    .Include(e => e.Bookings)
                    .Where(e => e.Status == EventStatus.Published)
                    .Select(e => new
                    {
                        EventId = e.EventId,
                        EventName = e.EventName,
                        Revenue = e.Bookings!.Where(b => b.PaymentStatus == PaymentStatus.Completed)
                                            .Sum(b => b.FinalAmount),
                        TicketsSold = e.Bookings!.Where(b => b.PaymentStatus == PaymentStatus.Completed)
                                                .SelectMany(b => b.BookingDetails!)
                                                .Sum(bd => bd.Quantity)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .Take(count)
                    .ToListAsync();

                return Json(new { success = true, data = topEvents });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string GetTimeAgo(DateTime? dateTime)
        {
            if (!dateTime.HasValue) return "Unknown";

            var timeSpan = DateTime.UtcNow - dateTime.Value;

            if (timeSpan.Days > 0)
                return $"{timeSpan.Days} day{(timeSpan.Days > 1 ? "s" : "")} ago";
            if (timeSpan.Hours > 0)
                return $"{timeSpan.Hours} hour{(timeSpan.Hours > 1 ? "s" : "")} ago";
            if (timeSpan.Minutes > 0)
                return $"{timeSpan.Minutes} minute{(timeSpan.Minutes > 1 ? "s" : "")} ago";

            return "Just now";
        }
    }
}