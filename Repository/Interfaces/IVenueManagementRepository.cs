using StarTickets.Models;

namespace StarTickets.Repositories.Interfaces
{
    public interface IVenueManagementRepository
    {
        IQueryable<Venue> QueryVenuesWithEvents();
        Task<List<string>> GetDistinctCitiesAsync();
        Task<Venue?> FindVenueAsync(int id);
        Task AddVenueAsync(Venue venue);
        Task SaveChangesAsync();
        Task<Venue?> GetVenueDetailsAsync(int id);
        Task<decimal> GetVenueTotalRevenueAsync(Venue venue);
        void RemoveVenue(Venue venue);
    }
}


