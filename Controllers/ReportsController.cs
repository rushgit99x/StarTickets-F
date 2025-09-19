using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Filters;
using StarTickets.Models.ViewModels;
using StarTickets.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StarTickets.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IPdfReportService _pdfService;

        public ReportsController(ApplicationDbContext db, IPdfReportService pdfService)
        {
            _db = db;
            _pdfService = pdfService;
        }

        [RoleAuthorize("1")]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var model = await GetReportsDataAsync(startDate, endDate);
            return View(model);
        }

        [RoleAuthorize("1")]
        public async Task<IActionResult> DownloadSalesReport(DateTime? startDate, DateTime? endDate)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var model = await GetReportsDataAsync(startDate, endDate);
            var pdfBytes = await _pdfService.GenerateSalesReportAsync(model);
            
            var fileName = $"Sales_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [RoleAuthorize("1")]
        public async Task<IActionResult> DownloadUsersReport(DateTime? startDate, DateTime? endDate)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var model = await GetReportsDataAsync(startDate, endDate);
            var pdfBytes = await _pdfService.GenerateUsersReportAsync(model);
            
            var fileName = $"Users_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [RoleAuthorize("1")]
        public async Task<IActionResult> DownloadEventsReport(DateTime? startDate, DateTime? endDate)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var model = await GetReportsDataAsync(startDate, endDate);
            var pdfBytes = await _pdfService.GenerateEventsReportAsync(model);
            
            var fileName = $"Events_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [RoleAuthorize("1")]
        public async Task<IActionResult> DownloadBookingsReport(DateTime? startDate, DateTime? endDate)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var model = await GetReportsDataAsync(startDate, endDate);
            var pdfBytes = await _pdfService.GenerateBookingsReportAsync(model);
            
            var fileName = $"Bookings_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [RoleAuthorize("1")]
        public async Task<IActionResult> DownloadFullReport(DateTime? startDate, DateTime? endDate)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var model = await GetReportsDataAsync(startDate, endDate);
            var pdfBytes = await _pdfService.GenerateFullReportAsync(model);
            
            var fileName = $"Complete_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        private async Task<ReportsViewModel> GetReportsDataAsync(DateTime? startDate, DateTime? endDate)
        {
            DateTime start = startDate?.Date ?? DateTime.UtcNow.AddDays(-30).Date;
            DateTime end = (endDate?.Date ?? DateTime.UtcNow.Date).AddDays(1).AddTicks(-1);

            var model = new ReportsViewModel
            {
                Filter = new ReportsFilterViewModel { StartDate = start, EndDate = end }
            };

            // KPIs over the selected range
            var bookingsInRange = _db.Bookings
                .AsNoTracking()
                .Where(b => b.BookingDate >= start && b.BookingDate <= end && b.PaymentStatus == Models.PaymentStatus.Completed);

            model.Kpis.TotalSales = await bookingsInRange.SumAsync(b => (decimal?)b.FinalAmount) ?? 0m;
            model.Kpis.TotalBookings = await bookingsInRange.CountAsync();

            model.Kpis.TotalEvents = await _db.Events.AsNoTracking().CountAsync();
            model.Kpis.TotalUsers = await _db.Users.AsNoTracking().CountAsync();

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1).AddTicks(-1);
            var bookingsToday = _db.Bookings.AsNoTracking()
                .Where(b => b.BookingDate >= today && b.BookingDate <= tomorrow && b.PaymentStatus == Models.PaymentStatus.Completed);
            model.Kpis.SalesToday = await bookingsToday.SumAsync(b => (decimal?)b.FinalAmount) ?? 0m;
            model.Kpis.BookingsToday = await bookingsToday.CountAsync();

            model.Kpis.EventsUpcoming = await _db.Events.AsNoTracking()
                .CountAsync(e => e.EventDate >= today && e.IsActive);

            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            model.Kpis.NewUsersThisMonth = await _db.Users.AsNoTracking()
                .CountAsync(u => u.CreatedAt >= monthStart);

            // Sales by date within range
            model.SalesByDate = await bookingsInRange
                .GroupBy(b => b.BookingDate.Date)
                .Select(g => new SalesByDateItem
                {
                    Date = g.Key,
                    Amount = g.Sum(x => x.FinalAmount)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Top events by sales within range
            model.TopEvents = await bookingsInRange
                .GroupBy(b => new { b.EventId, b.Event!.EventName })
                .Select(g => new TopEventSalesItem
                {
                    EventId = g.Key.EventId,
                    EventName = g.Key.EventName,
                    Sales = g.Sum(x => x.FinalAmount),
                    Bookings = g.Count()
                })
                .OrderByDescending(x => x.Sales)
                .Take(10)
                .ToListAsync();

            return model;
        }
    }
}


