using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Repositories.Interfaces;

namespace StarTickets.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ApplicationDbContext _db;

        public AdminRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public int GetTotalUsers()
        {
            return _db.Users.Count();
        }

        public int GetActiveEvents()
        {
            return _db.Events.Count(e => e.IsActive && e.Status == EventStatus.Published);
        }

        public int GetTicketsSold()
        {
            return _db.BookingDetails.Sum(bd => (int?)bd.Quantity) ?? 0;
        }

        public decimal GetTotalRevenue()
        {
            return _db.Bookings.Where(b => b.PaymentStatus == PaymentStatus.Completed)
                .Sum(b => (decimal?)b.FinalAmount) ?? 0m;
        }

        public User? GetUserById(int userId)
        {
            return _db.Users.FirstOrDefault(u => u.UserId == userId);
        }

        public Dictionary<int, decimal> GetMonthlyRevenueByMonth(int year)
        {
            return _db.Bookings
                .Where(b => b.PaymentStatus == PaymentStatus.Completed && b.BookingDate.Year == year)
                .AsEnumerable()
                .GroupBy(b => b.BookingDate.Month)
                .ToDictionary(g => g.Key, g => g.Sum(b => b.FinalAmount));
        }

        public List<(int EventId, string EventName, string OrganizerName, DateTime EventDate, string VenueName, EventStatus Status)> GetRecentEventsBasic(int take)
        {
            var recent = _db.Events
                .Include(e => e.Organizer)
                .Include(e => e.Venue)
                .OrderByDescending(e => e.EventDate)
                .Take(take)
                .Select(e => new
                {
                    e.EventId,
                    e.EventName,
                    OrganizerName = e.Organizer != null ? (e.Organizer.FirstName + " " + e.Organizer.LastName) : "",
                    e.EventDate,
                    VenueName = e.Venue != null ? e.Venue.VenueName : "",
                    e.Status
                })
                .AsNoTracking()
                .ToList();

            return recent.Select(e => (e.EventId, e.EventName, e.OrganizerName, e.EventDate, e.VenueName, e.Status)).ToList();
        }

        public Dictionary<int, int> GetTicketsSoldByEventIds(IEnumerable<int> eventIds)
        {
            var ids = eventIds.ToList();
            return _db.Bookings
                .Where(b => ids.Contains(b.EventId) && b.PaymentStatus == PaymentStatus.Completed)
                .Join(_db.BookingDetails, b => b.BookingId, bd => bd.BookingId, (b, bd) => new { b.EventId, bd.Quantity })
                .AsEnumerable()
                .GroupBy(x => x.EventId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
        }

        public Dictionary<int, decimal> GetRevenueByEventIds(IEnumerable<int> eventIds)
        {
            var ids = eventIds.ToList();
            return _db.Bookings
                .Where(b => ids.Contains(b.EventId) && b.PaymentStatus == PaymentStatus.Completed)
                .AsEnumerable()
                .GroupBy(b => b.EventId)
                .ToDictionary(g => g.Key, g => g.Sum(b => b.FinalAmount));
        }
    }
}


