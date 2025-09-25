using StarTickets.Models;

namespace StarTickets.Repositories.Interfaces
{
    public interface IUserManagementRepository
    {
        IQueryable<User> QueryUsersWithRole();
        Task<List<UserRole>> GetUserRolesAsync();
        Task<bool> EmailExistsAsync(string email, int? excludeUserId = null);
        Task AddUserAsync(User user);
        Task<User?> FindUserAsync(int id);
        Task SaveChangesAsync();
        void UpdateUser(User user);
        Task<bool> HasBookingsAsync(int userId);
        Task<bool> HasEventsAsync(int userId);
        Task<List<User>> GetUsersByIdsAsync(IEnumerable<int> userIds);
        void UpdateUsers(IEnumerable<User> users);
        void RemoveUsers(IEnumerable<User> users);
        Task<List<User>> GetAllUsersWithRoleOrderedAsync();
        Task<int> CountUsersAsync();
        Task<int> CountActiveUsersAsync();
        Task<int> CountInactiveUsersAsync();
        Task<int> CountUsersByRoleAsync(int roleId);
        Task<int> CountRecentUsersAsync(int days);
    }
}


