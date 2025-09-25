using StarTickets.Models;
using StarTickets.Models.ViewModels;

namespace StarTickets.Services.Interfaces
{
    public interface IBookingService
    {
        Task<BookTicketViewModel?> PrepareBookingAsync(int eventId, int userId);
        Task<(bool Success, string? Error, int? BookingId)> ProcessBookingAsync(BookTicketViewModel model, int userId);
        Task<Booking?> GetBookingConfirmationAsync(int bookingId, int userId);
        Task<(bool Success, string? Error)> ProcessPaymentAsync(PaymentViewModel paymentModel, int userId);
        Task<(bool Success, string Message)> ValidatePromoAsync(string promoCode, decimal totalAmount);
        Task<List<object>?> GetTicketDataWithQRAsync(int bookingId, int userId);
        string GenerateQRCodeDataUrl(string qrData);
        Task<(bool Success, string Message)> EmailTicketsAsync(int bookingId, int userId);
        Task<(bool Success, byte[]? PdfBytes)> DownloadTicketAsync(string ticketNumber, int userId);
    }
}


