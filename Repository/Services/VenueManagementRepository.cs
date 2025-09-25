using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Repositories.Interfaces;

namespace StarTickets.Repositories
{
    public class VenueManagementRepository : IVenueManagementRepository
    {
        private readonly ApplicationDbContext _context;

        public VenueManagementRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<Venue> QueryVenuesWithEvents()
        {
            return _context.Venues.Include(v => v.Events).AsQueryable();
        }

        public async Task<List<string>> GetDistinctCitiesAsync()
        {
            return await _context.Venues.Where(v => !string.IsNullOrEmpty(v.City))
                .Select(v => v.City).Distinct().OrderBy(c => c).ToListAsync();
        }

        public Task<Venue?> FindVenueAsync(int id)
        {
            return _context.Venues.FirstOrDefaultAsync(v => v.VenueId == id);
        }

        public async Task AddVenueAsync(Venue venue)
        {
            _context.Venues.Add(venue);
            await _context.SaveChangesAsync();
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public Task<Venue?> GetVenueDetailsAsync(int id)
        {
            return _context.Venues
                .Include(v => v.Events!.Where(e => e.IsActive))
                .ThenInclude(e => e.Category)
                .FirstOrDefaultAsync(v => v.VenueId == id);
        }

        public async Task<decimal> GetVenueTotalRevenueAsync(Venue venue)
        {
            return await _context.Bookings
                .Where(b => venue.Events!.Select(e => e.EventId).Contains(b.EventId) && b.PaymentStatus == PaymentStatus.Completed)
                .SumAsync(b => b.FinalAmount);
        }

        public void RemoveVenue(Venue venue)
        {
            _context.Venues.Remove(venue);
        }
    }
}


