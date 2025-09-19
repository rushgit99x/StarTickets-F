using StarTickets.Models.ViewModels;

namespace StarTickets.Services
{
    public interface IPdfReportService
    {
        Task<byte[]> GenerateSalesReportAsync(ReportsViewModel model);
        Task<byte[]> GenerateUsersReportAsync(ReportsViewModel model);
        Task<byte[]> GenerateEventsReportAsync(ReportsViewModel model);
        Task<byte[]> GenerateBookingsReportAsync(ReportsViewModel model);
        Task<byte[]> GenerateFullReportAsync(ReportsViewModel model);
    }
}

