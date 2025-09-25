using StarTickets.Models;
using StarTickets.Models.ViewModels;

namespace StarTickets.Services.Interfaces
{
    public interface ICategoryManagementService
    {
        Task<CategoryManagementViewModel> GetIndexAsync(string searchTerm, int page, int pageSize);
        Task<bool> CreateAsync(CreateCategoryViewModel model);
        Task<EditCategoryViewModel?> GetEditAsync(int id);
        Task<bool> EditAsync(EditCategoryViewModel model);
        Task<CategoryDetailsViewModel?> GetDetailsAsync(int id);
        Task<(bool Success, string Message)> DeleteAsync(int id);
        Task<(bool Success, string Message)> DeleteAjaxAsync(int id);
        Task<EventCategory?> GetCategoryForDeleteAsync(int id);
    }
}