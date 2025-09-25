using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Repositories.Interfaces;

namespace StarTickets.Repositories
{
    public class BookingManagementRepository : IBookingManagementRepository
    {
        private readonly ApplicationDbContext _context;

        public BookingManagementRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<Booking> QueryBookingsWithDetails()
        {
            return _context.Bookings
                .Include(b => b.Event)!.ThenInclude(e => e!.Venue)
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails)!.ThenInclude(d => d.TicketCategory)
                .Include(b => b.BookingDetails)!.ThenInclude(d => d.Tickets)
                .AsQueryable();
        }

        public Task<Booking?> GetBookingWithFullDetailsAsync(int id)
        {
            return _context.Bookings
                .Include(b => b.Event)!.ThenInclude(e => e!.Venue)
                .Include(b => b.Event)!.ThenInclude(e => e!.Category)
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails)!.ThenInclude(d => d.TicketCategory)
                .Include(b => b.BookingDetails)!.ThenInclude(d => d.Tickets)
                .FirstOrDefaultAsync(b => b.BookingId == id);
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();
        public void RemoveTickets(IEnumerable<Ticket> tickets) => _context.Tickets.RemoveRange(tickets);
        public void RemoveBookingDetails(IEnumerable<BookingDetail> details) => _context.BookingDetails.RemoveRange(details);
        public void RemoveBooking(Booking booking) => _context.Bookings.Remove(booking);
    }
}


