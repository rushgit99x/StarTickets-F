using StarTickets.Models;
using StarTickets.Models.ViewModels;

namespace StarTickets.Services.Interfaces
{
    public interface IEventManagementService
    {
        Task<EventManagementViewModel> GetIndexAsync(string searchTerm, int categoryFilter, EventStatus? statusFilter, int page, int pageSize);
        Task<CreateEventViewModel> GetCreateFormAsync();
        Task<bool> CreateAsync(CreateEventViewModel model);
        Task<EditEventViewModel?> GetEditFormAsync(int id);
        Task<bool> EditAsync(EditEventViewModel model);
        Task<EventDetailsViewModel?> GetDetailsAsync(int id);
        Task<(bool Success, string Message)> UpdateStatusAsync(int id, EventStatus status);
        Task<(bool Success, string Message)> DeleteAsync(int id);
    }
}


