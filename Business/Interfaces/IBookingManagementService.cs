using StarTickets.Models;
using StarTickets.Models.ViewModels;

namespace StarTickets.Services.Interfaces
{
    public interface IBookingManagementService
    {
        Task<AdminBookingListViewModel> GetIndexAsync(string search, PaymentStatus? paymentStatus, BookingStatus? bookingStatus, int page, int pageSize);
        Task<BookingDetailsViewModel?> GetDetailsAsync(int id);
        Task<(bool Success, string Message)> CancelAsync(int id, string? reason);
        Task<(bool Success, string Message)> DeleteAsync(int id);
    }
}


