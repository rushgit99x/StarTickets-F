using Microsoft.EntityFrameworkCore.Storage;
using StarTickets.Models;

namespace StarTickets.Repositories.Interfaces
{
    public interface IBookingRepository
    {
        Task<Event?> GetPublishedEventForBookingAsync(int eventId);
        Task<Event?> GetEventForReloadAsync(int eventId);
        Task<User?> GetUserByIdAsync(int userId);

        Task AddBookingAsync(Booking booking);
        void AddTickets(IEnumerable<Ticket> tickets);
        Task SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();

        Task<Booking?> GetCompletedBookingForEmailAsync(int bookingId, int customerId);
        Task<Booking?> GetPendingBookingForPaymentAsync(int bookingId, int customerId);
        Task<Booking?> GetBookingWithTicketsAsync(int bookingId, int customerId);

        Task<PromotionalCampaign?> GetActivePromoByCodeAsync(string code);
        Task<PromotionalCampaign?> GetPromoByIdAsync(int promoId);
        Task<Ticket?> GetTicketByNumberForUserAsync(string ticketNumber, int userId);
    }
}


