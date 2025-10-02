using StarTickets.Models;

namespace StarTickets.Repositories.Interfaces
{
    public interface ICustomerRepository
    {
        Task<User?> GetCustomerByIdAsync(int customerId);
        Task<List<Booking>> GetCustomerBookingsAsync(int customerId);
        Task<List<Booking>> GetCustomerBookingsWithDetailsAsync(int customerId);
        Task<Booking?> GetBookingByReferenceAsync(string bookingReference, int customerId);
        Task<bool> DeleteBookingByReferenceAsync(string bookingReference, int customerId);
        Task<Ticket?> GetTicketByNumberForCustomerAsync(string ticketNumber, int customerId);
    }
}
