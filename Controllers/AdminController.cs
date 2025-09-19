using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Filters;
using StarTickets.Models;
using StarTickets.Models.ViewModels;

namespace StarTickets.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        [RoleAuthorize("1")]
        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            // Build dashboard model
            var model = new AdminDashboardViewModel
            {
                TotalUsers = _db.Users.Count(),
                ActiveEvents = _db.Events.Count(e => e.IsActive && e.Status == EventStatus.Published),
                TicketsSold = _db.BookingDetails.Sum(bd => (int?)bd.Quantity) ?? 0,
                TotalRevenue = _db.Bookings.Where(b => b.PaymentStatus == PaymentStatus.Completed).Sum(b => (decimal?)b.FinalAmount) ?? 0m,
                MonthlyLabels = Enumerable.Range(0, 12).Select(i => System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(i + 1)).ToList()
            };

            // Populate current admin identity
            var currentUser = _db.Users.FirstOrDefault(u => u.UserId == userId);
            if (currentUser != null)
            {
                model.AdminFullName = (currentUser.FirstName + " " + currentUser.LastName).Trim();
                model.AdminEmail = currentUser.Email;
                var initials = (currentUser.FirstName?.Trim().Length > 0 ? currentUser.FirstName.Trim()[0].ToString() : "") +
                               (currentUser.LastName?.Trim().Length > 0 ? currentUser.LastName.Trim()[0].ToString() : "");
                model.AdminInitials = string.IsNullOrWhiteSpace(initials) ? "AD" : initials.ToUpper();
            }

            // Monthly revenue for the current year
            var currentYear = DateTime.UtcNow.Year;
            var revenueByMonth = _db.Bookings
                .Where(b => b.PaymentStatus == PaymentStatus.Completed && b.BookingDate.Year == currentYear)
                .AsEnumerable() // switch to client to use Month safely across providers
                .GroupBy(b => b.BookingDate.Month)
                .ToDictionary(g => g.Key, g => g.Sum(b => b.FinalAmount));

            model.MonthlyRevenue = Enumerable.Range(1, 12)
                .Select(m => revenueByMonth.ContainsKey(m) ? revenueByMonth[m] : 0m)
                .ToList();

            // Recent events sample: latest 3 events with basic aggregates
            var recentEvents = _db.Events
                .Include(e => e.Organizer)
                .Include(e => e.Venue)
                .OrderByDescending(e => e.EventDate)
                .Take(3)
                .Select(e => new
                {
                    e.EventName,
                    OrganizerName = e.Organizer != null ? (e.Organizer.FirstName + " " + e.Organizer.LastName) : "",
                    e.EventDate,
                    VenueName = e.Venue != null ? e.Venue.VenueName : "",
                    e.Status,
                    EventId = e.EventId
                })
                .AsNoTracking()
                .ToList();

            var recentEventIds = recentEvents.Select(x => x.EventId).ToList();

            var ticketsByEvent = _db.Bookings
                .Where(b => recentEventIds.Contains(b.EventId) && b.PaymentStatus == PaymentStatus.Completed)
                .Join(_db.BookingDetails, b => b.BookingId, bd => bd.BookingId, (b, bd) => new { b.EventId, bd.Quantity })
                .AsEnumerable()
                .GroupBy(x => x.EventId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            var revenueByEvent = _db.Bookings
                .Where(b => recentEventIds.Contains(b.EventId) && b.PaymentStatus == PaymentStatus.Completed)
                .AsEnumerable()
                .GroupBy(b => b.EventId)
                .ToDictionary(g => g.Key, g => g.Sum(b => b.FinalAmount));

            foreach (var e in recentEvents)
            {
                model.RecentEvents.Add(new RecentEventViewModel
                {
                    EventName = e.EventName,
                    OrganizerName = e.OrganizerName,
                    EventDate = e.EventDate,
                    VenueName = e.VenueName,
                    TicketsSold = ticketsByEvent.ContainsKey(e.EventId) ? ticketsByEvent[e.EventId] : 0,
                    Revenue = revenueByEvent.ContainsKey(e.EventId) ? revenueByEvent[e.EventId] : 0m,
                    Status = e.Status.ToString()
                });
            }

            return View(model);
        }
    }
}
