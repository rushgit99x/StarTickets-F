using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Repositories.Interfaces;

namespace StarTickets.Repositories
{
    public class ReportsRepository : IReportsRepository
    {
        private readonly ApplicationDbContext _db;

        public ReportsRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public IQueryable<Booking> QueryCompletedBookingsInRange(DateTime start, DateTime end)
        {
            return _db.Bookings.AsNoTracking()
                .Where(b => b.BookingDate >= start && b.BookingDate <= end && b.PaymentStatus == PaymentStatus.Completed);
        }

        public async Task<int> CountEventsAsync()
        {
            return await _db.Events.AsNoTracking().CountAsync();
        }

        public async Task<int> CountUsersAsync()
        {
            return await _db.Users.AsNoTracking().CountAsync();
        }

        public async Task<int> CountUpcomingEventsAsync(DateTime today)
        {
            return await _db.Events.AsNoTracking().CountAsync(e => e.EventDate >= today && e.IsActive);
        }

        public async Task<int> CountNewUsersSinceAsync(DateTime monthStart)
        {
            return await _db.Users.AsNoTracking().CountAsync(u => u.CreatedAt >= monthStart);
        }
    }
}


