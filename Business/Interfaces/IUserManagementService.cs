using StarTickets.Models;
using StarTickets.Models.ViewModels;

namespace StarTickets.Services.Interfaces
{
    public interface IUserManagementService
    {
        Task<UserManagementViewModel> GetIndexAsync(string search, int page, int pageSize, int? roleFilter, bool? activeFilter);
        Task<CreateUserViewModel> GetCreateAsync();
        Task<(bool Success, string Message)> CreateAsync(CreateUserViewModel model);
        Task<EditUserViewModel?> GetEditAsync(int id);
        Task<(bool Success, string Message)> EditAsync(EditUserViewModel model);
        Task<UserDetailsViewModel?> GetDetailsAsync(int id);
        Task<(bool Success, string Message, bool? IsActive)> ToggleStatusAsync(int id);
        Task<(bool Success, string Message)> ResetPasswordAsync(int id, string newPassword);
        Task<(bool Success, string Message)> ResendWelcomeEmailAsync(int id);
        Task<object> GetUserStatsAsync();
        Task<(bool Success, string Message)> BulkActionAsync(string action, int[] userIds);
        Task<(bool Success, string Message)> DeleteAsync(int id, bool forceDelete);
        Task<string> GenerateUsersCsvAsync();
    }
}


