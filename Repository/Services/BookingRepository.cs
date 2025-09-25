using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Repositories.Interfaces;

namespace StarTickets.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly ApplicationDbContext _context;

        public BookingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Event?> GetPublishedEventForBookingAsync(int eventId)
        {
            return await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.TicketCategories.Where(tc => tc.IsActive))
                .FirstOrDefaultAsync(e => e.EventId == eventId && e.IsActive && e.Status == EventStatus.Published);
        }

        public async Task<Event?> GetEventForReloadAsync(int eventId)
        {
            return await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.TicketCategories.Where(tc => tc.IsActive))
                .FirstOrDefaultAsync(e => e.EventId == eventId);
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task AddBookingAsync(Booking booking)
        {
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
        }

        public void AddTickets(IEnumerable<Ticket> tickets)
        {
            _context.Tickets.AddRange(tickets);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task<Booking?> GetCompletedBookingForEmailAsync(int bookingId, int customerId)
        {
            return await _context.Bookings
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue)
                .Include(b => b.Event)
                    .ThenInclude(e => e.Category)
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.TicketCategory)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Tickets)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId &&
                                         b.CustomerId == customerId &&
                                         b.PaymentStatus == PaymentStatus.Completed);
        }

        public async Task<Booking?> GetPendingBookingForPaymentAsync(int bookingId, int customerId)
        {
            return await _context.Bookings
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue)
                .Include(b => b.Event)
                    .ThenInclude(e => e.Category)
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.TicketCategory)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Tickets)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId &&
                                         b.CustomerId == customerId &&
                                         b.PaymentStatus == PaymentStatus.Pending);
        }

        public async Task<Booking?> GetBookingWithTicketsAsync(int bookingId, int customerId)
        {
            return await _context.Bookings
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.TicketCategory)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.Tickets)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == customerId);
        }

        public async Task<PromotionalCampaign?> GetActivePromoByCodeAsync(string code)
        {
            return await _context.PromotionalCampaigns
                .FirstOrDefaultAsync(p => p.DiscountCode == code && p.IsActive && DateTime.UtcNow >= p.StartDate && DateTime.UtcNow <= p.EndDate && (p.MaxUsage == null || p.CurrentUsage < p.MaxUsage));
        }

        public async Task<PromotionalCampaign?> GetPromoByIdAsync(int promoId)
        {
            return await _context.PromotionalCampaigns.FindAsync(promoId);
        }

        public async Task<Ticket?> GetTicketByNumberForUserAsync(string ticketNumber, int userId)
        {
            return await _context.Tickets
                .Include(t => t.BookingDetail)
                    .ThenInclude(bd => bd.Booking)
                        .ThenInclude(b => b.Event)
                .Include(t => t.BookingDetail.TicketCategory)
                .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber && t.BookingDetail.Booking.CustomerId == userId);
        }
    }
}


