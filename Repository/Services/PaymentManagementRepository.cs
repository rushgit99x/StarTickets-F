using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Repositories.Interfaces;

namespace StarTickets.Repositories
{
    public class PaymentManagementRepository : IPaymentManagementRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentManagementRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<Booking> QueryBookingsWithCustomerAndEvent()
        {
            return _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Event)
                .AsQueryable();
        }

        public async Task<List<(int EventId, string EventName)>> GetEventOptionsAsync()
        {
            var list = await _context.Events
                .OrderBy(e => e.EventName)
                .Select(e => new { e.EventId, e.EventName })
                .ToListAsync();
            return list.Select(e => (e.EventId, e.EventName)).ToList();
        }
    }
}


