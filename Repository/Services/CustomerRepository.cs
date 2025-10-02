using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Repositories.Interfaces;

namespace StarTickets.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly ApplicationDbContext _context;

        public CustomerRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetCustomerByIdAsync(int customerId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == customerId && u.Role == RoleConstants.CustomerId);
        }

        public async Task<List<Booking>> GetCustomerBookingsAsync(int customerId)
        {
            return await _context.Bookings
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetCustomerBookingsWithDetailsAsync(int customerId)
        {
            return await _context.Bookings
                .Include(b => b.Event)!
                    .ThenInclude(e => e!.Venue)
                .Include(b => b.BookingDetails)!
                    .ThenInclude(bd => bd.Tickets)
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<Booking?> GetBookingByReferenceAsync(string bookingReference, int customerId)
        {
            return await _context.Bookings
                .Include(b => b.Event)!
                    .ThenInclude(e => e!.Venue)
                .Include(b => b.BookingDetails)!
                    .ThenInclude(bd => bd.Tickets)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.BookingReference == bookingReference && b.CustomerId == customerId);
        }

        public async Task<bool> DeleteBookingByReferenceAsync(string bookingReference, int customerId)
        {
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingReference == bookingReference && b.CustomerId == customerId);

            if (booking == null)
                return false;

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Ticket?> GetTicketByNumberForCustomerAsync(string ticketNumber, int customerId)
        {
            return await _context.Tickets
                .Include(t => t.BookingDetail)!
                    .ThenInclude(bd => bd.Booking)!
                        .ThenInclude(b => b.Event)!
                            .ThenInclude(e => e.Venue)
                .Include(t => t.BookingDetail)!
                    .ThenInclude(bd => bd.TicketCategory)
                .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber &&
                    t.BookingDetail!.Booking!.CustomerId == customerId);
        }
    }
}
