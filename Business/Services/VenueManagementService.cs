using Microsoft.EntityFrameworkCore;
using StarTickets.Models;
using StarTickets.Models.ViewModels;
using StarTickets.Repositories.Interfaces;
using StarTickets.Services.Interfaces;

namespace StarTickets.Services
{
    public class VenueManagementService : IVenueManagementService
    {
        private readonly IVenueManagementRepository _repo;
        private readonly ILogger<VenueManagementService> _logger;

        public VenueManagementService(IVenueManagementRepository repo, ILogger<VenueManagementService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<VenueManagementViewModel> GetIndexAsync(string searchTerm, string cityFilter, bool? activeFilter, int page, int pageSize)
        {
            var query = _repo.QueryVenuesWithEvents();
            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(v => v.VenueName.Contains(searchTerm) || v.Address.Contains(searchTerm) || v.City.Contains(searchTerm) || v.Country.Contains(searchTerm));
            if (!string.IsNullOrWhiteSpace(cityFilter))
                query = query.Where(v => v.City == cityFilter);
            if (activeFilter.HasValue)
                query = query.Where(v => v.IsActive == activeFilter.Value);

            var totalVenues = await query.CountAsync();
            var venues = await query.OrderBy(v => v.VenueName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            var cities = await _repo.GetDistinctCitiesAsync();

            return new VenueManagementViewModel
            {
                Venues = venues,
                Cities = cities,
                SearchTerm = searchTerm,
                CityFilter = cityFilter,
                ActiveFilter = activeFilter,
                CurrentPage = page,
                PageSize = pageSize,
                TotalVenues = totalVenues,
                TotalPages = (int)Math.Ceiling((double)totalVenues / pageSize)
            };
        }

        public async Task<bool> CreateAsync(CreateVenueViewModel model)
        {
            try
            {
                var venue = new Venue
                {
                    VenueName = model.VenueName,
                    Address = model.Address,
                    City = model.City,
                    State = model.State,
                    Country = model.Country,
                    PostalCode = model.PostalCode,
                    Capacity = model.Capacity,
                    Facilities = model.Facilities,
                    ContactPhone = model.ContactPhone,
                    ContactEmail = model.ContactEmail,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _repo.AddVenueAsync(venue);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating venue");
                return false;
            }
        }

        public async Task<EditVenueViewModel?> GetEditAsync(int id)
        {
            var venue = await _repo.FindVenueAsync(id);
            if (venue == null) return null;
            return new EditVenueViewModel
            {
                VenueId = venue.VenueId,
                VenueName = venue.VenueName,
                Address = venue.Address,
                City = venue.City,
                State = venue.State,
                Country = venue.Country,
                PostalCode = venue.PostalCode,
                Capacity = venue.Capacity,
                Facilities = venue.Facilities,
                ContactPhone = venue.ContactPhone,
                ContactEmail = venue.ContactEmail,
                IsActive = venue.IsActive
            };
        }

        public async Task<bool> EditAsync(EditVenueViewModel model)
        {
            try
            {
                var venue = await _repo.FindVenueAsync(model.VenueId);
                if (venue == null) return false;
                venue.VenueName = model.VenueName;
                venue.Address = model.Address;
                venue.City = model.City;
                venue.State = model.State;
                venue.Country = model.Country;
                venue.PostalCode = model.PostalCode;
                venue.Capacity = model.Capacity;
                venue.Facilities = model.Facilities;
                venue.ContactPhone = model.ContactPhone;
                venue.ContactEmail = model.ContactEmail;
                venue.IsActive = model.IsActive;
                venue.UpdatedAt = DateTime.UtcNow;
                await _repo.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating venue");
                return false;
            }
        }

        public async Task<VenueDetailsViewModel?> GetDetailsAsync(int id)
        {
            var venue = await _repo.GetVenueDetailsAsync(id);
            if (venue == null) return null;
            var totalEvents = venue.Events?.Count ?? 0;
            var upcomingEvents = venue.Events?.Count(e => e.EventDate > DateTime.UtcNow) ?? 0;
            var pastEvents = venue.Events?.Count(e => e.EventDate <= DateTime.UtcNow) ?? 0;
            var totalRevenue = await _repo.GetVenueTotalRevenueAsync(venue);
            return new VenueDetailsViewModel
            {
                Venue = venue,
                TotalEvents = totalEvents,
                UpcomingEvents = upcomingEvents,
                PastEvents = pastEvents,
                TotalRevenue = totalRevenue,
                RecentEvents = venue.Events?.OrderByDescending(e => e.CreatedAt).Take(10).ToList() ?? new List<Event>()
            };
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            try
            {
                var venue = await _repo.FindVenueAsync(id);
                if (venue == null) return (false, "Venue not found.");
                var withEvents = await _repo.GetVenueDetailsAsync(id);
                if (withEvents?.Events?.Any() == true)
                    return (false, "Cannot delete venue with existing events. Please remove or reassign events first.");
                _repo.RemoveVenue(venue);
                await _repo.SaveChangesAsync();
                return (true, "Venue deleted successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting venue");
                return (false, "An error occurred while deleting the venue.");
            }
        }
    }
}


