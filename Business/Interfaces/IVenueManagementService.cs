using StarTickets.Models.ViewModels;

namespace StarTickets.Services.Interfaces
{
    public interface IVenueManagementService
    {
        Task<VenueManagementViewModel> GetIndexAsync(string searchTerm, string cityFilter, bool? activeFilter, int page, int pageSize);
        Task<bool> CreateAsync(CreateVenueViewModel model);
        Task<EditVenueViewModel?> GetEditAsync(int id);
        Task<bool> EditAsync(EditVenueViewModel model);
        Task<VenueDetailsViewModel?> GetDetailsAsync(int id);
        Task<(bool Success, string Message)> DeleteAsync(int id);
    }
}


