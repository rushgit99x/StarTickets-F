using StarTickets.Models;
using StarTickets.Models.ViewModels;

namespace StarTickets.Services
{
    public interface ICustomerService
    {
        Task<CustomerDashboardViewModel> GetDashboardDataAsync(int customerId);
        Task<List<BookingViewModel>> GetCustomerBookingsAsync(int customerId, int? year = null, int? status = null);
        Task<List<EventViewModel>> GetCustomerUpcomingEventsAsync(int customerId);
        Task<List<BookingViewModel>> GetBookingHistoryAsync(int customerId, int? year = null, int? status = null);
        Task<List<EventViewModel>> GetEventsToRateAsync(int customerId);
        Task<bool> RateEventAsync(int customerId, RateEventViewModel model);
        Task<bool> UpdateProfileAsync(int customerId, UpdateProfileViewModel model);
        Task<User?> GetCustomerProfileAsync(int customerId);
        Task<byte[]?> GenerateTicketPdfAsync(string bookingReference, int customerId);
        Task<byte[]?> GenerateTicketPdfByTicketNumberAsync(string ticketNumber, int customerId);
        Task<bool> EmailTicketAsync(string bookingReference, int customerId);
        Task<bool> EmailTicketByTicketNumberAsync(string ticketNumber, int customerId);
        Task<List<byte[]>> GetAllCustomerTicketsAsync(int customerId);
        Task<DashboardStatsViewModel> GetDashboardStatsAsync(int customerId);
        Task<bool> DeleteBookingAsync(string bookingReference, int value);
        Task<List<object>> GetTicketsWithQRCodesAsync(int customerId);
        Task<bool> SetEventReminderAsync(int customerId, int eventId, DateTime reminderTime);
        Task<List<EventViewModel>> GetUpcomingEventsForRemindersAsync();
    }
}