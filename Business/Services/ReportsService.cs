using Microsoft.EntityFrameworkCore;
using StarTickets.Models.ViewModels;
using StarTickets.Repositories.Interfaces;
using StarTickets.Services.Interfaces;

namespace StarTickets.Services
{
    public class ReportsService : IReportsService
    {
        private readonly IReportsRepository _repo;
        private readonly IPdfReportService _pdf;

        public ReportsService(IReportsRepository repo, IPdfReportService pdf)
        {
            _repo = repo;
            _pdf = pdf;
        }

        public async Task<ReportsViewModel> BuildReportsAsync(DateTime? startDate, DateTime? endDate)
        {
            DateTime start = startDate?.Date ?? DateTime.UtcNow.AddDays(-30).Date;
            DateTime end = (endDate?.Date ?? DateTime.UtcNow.Date).AddDays(1).AddTicks(-1);

            var model = new ReportsViewModel
            {
                Filter = new ReportsFilterViewModel { StartDate = start, EndDate = end }
            };

            var bookingsInRange = _repo.QueryCompletedBookingsInRange(start, end);
            model.Kpis.TotalSales = await bookingsInRange.SumAsync(b => (decimal?)b.FinalAmount) ?? 0m;
            model.Kpis.TotalBookings = await bookingsInRange.CountAsync();
            model.Kpis.TotalEvents = await _repo.CountEventsAsync();
            model.Kpis.TotalUsers = await _repo.CountUsersAsync();

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1).AddTicks(-1);
            var bookingsToday = _repo.QueryCompletedBookingsInRange(today, tomorrow);
            model.Kpis.SalesToday = await bookingsToday.SumAsync(b => (decimal?)b.FinalAmount) ?? 0m;
            model.Kpis.BookingsToday = await bookingsToday.CountAsync();

            model.Kpis.EventsUpcoming = await _repo.CountUpcomingEventsAsync(today);
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            model.Kpis.NewUsersThisMonth = await _repo.CountNewUsersSinceAsync(monthStart);

            model.SalesByDate = await bookingsInRange
                .GroupBy(b => b.BookingDate.Date)
                .Select(g => new SalesByDateItem
                {
                    Date = g.Key,
                    Amount = g.Sum(x => x.FinalAmount)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

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

        public Task<byte[]> GenerateSalesReportAsync(ReportsViewModel model) => _pdf.GenerateSalesReportAsync(model);
        public Task<byte[]> GenerateUsersReportAsync(ReportsViewModel model) => _pdf.GenerateUsersReportAsync(model);
        public Task<byte[]> GenerateEventsReportAsync(ReportsViewModel model) => _pdf.GenerateEventsReportAsync(model);
        public Task<byte[]> GenerateBookingsReportAsync(ReportsViewModel model) => _pdf.GenerateBookingsReportAsync(model);
        public Task<byte[]> GenerateFullReportAsync(ReportsViewModel model) => _pdf.GenerateFullReportAsync(model);
    }
}


