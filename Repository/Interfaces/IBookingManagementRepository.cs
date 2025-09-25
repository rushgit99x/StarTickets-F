using StarTickets.Models;

namespace StarTickets.Repositories.Interfaces
{
    public interface IBookingManagementRepository
    {
        IQueryable<Booking> QueryBookingsWithDetails();
        Task<Booking?> GetBookingWithFullDetailsAsync(int id);
        Task SaveChangesAsync();
        void RemoveTickets(IEnumerable<Ticket> tickets);
        void RemoveBookingDetails(IEnumerable<BookingDetail> details);
        void RemoveBooking(Booking booking);
    }
}


