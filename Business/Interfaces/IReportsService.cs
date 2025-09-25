using StarTickets.Models.ViewModels;

namespace StarTickets.Services.Interfaces
{
    public interface IReportsService
    {
        Task<ReportsViewModel> BuildReportsAsync(DateTime? startDate, DateTime? endDate);
        Task<byte[]> GenerateSalesReportAsync(ReportsViewModel model);
        Task<byte[]> GenerateUsersReportAsync(ReportsViewModel model);
        Task<byte[]> GenerateEventsReportAsync(ReportsViewModel model);
        Task<byte[]> GenerateBookingsReportAsync(ReportsViewModel model);
        Task<byte[]> GenerateFullReportAsync(ReportsViewModel model);
    }
}


