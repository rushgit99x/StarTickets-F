using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Repositories.Interfaces;

namespace StarTickets.Repositories
{
    public class UserManagementRepository : IUserManagementRepository
    {
        private readonly ApplicationDbContext _context;

        public UserManagementRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<User> QueryUsersWithRole()
        {
            return _context.Users.Include(u => u.UserRole).AsQueryable();
        }

        public async Task<List<UserRole>> GetUserRolesAsync()
        {
            return await _context.UserRoles.ToListAsync();
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludeUserId = null)
        {
            var q = _context.Users.AsQueryable();
            if (excludeUserId.HasValue) q = q.Where(u => u.UserId != excludeUserId.Value);
            return await q.AnyAsync(u => u.Email == email);
        }

        public async Task AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public Task<User?> FindUserAsync(int id)
        {
            return _context.Users.FindAsync(id).AsTask();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public void UpdateUser(User user)
        {
            _context.Update(user);
        }

        public Task<bool> HasBookingsAsync(int userId)
        {
            return _context.Bookings.AnyAsync(b => b.CustomerId == userId);
        }

        public Task<bool> HasEventsAsync(int userId)
        {
            return _context.Events.AnyAsync(e => e.OrganizerId == userId);
        }

        public async Task<List<User>> GetUsersByIdsAsync(IEnumerable<int> userIds)
        {
            return await _context.Users.Where(u => userIds.Contains(u.UserId)).ToListAsync();
        }

        public void UpdateUsers(IEnumerable<User> users)
        {
            _context.UpdateRange(users);
        }

        public void RemoveUsers(IEnumerable<User> users)
        {
            _context.Users.RemoveRange(users);
        }

        public async Task<List<User>> GetAllUsersWithRoleOrderedAsync()
        {
            return await _context.Users.Include(u => u.UserRole)
                .OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();
        }

        public Task<int> CountUsersAsync() => _context.Users.CountAsync();
        public Task<int> CountActiveUsersAsync() => _context.Users.CountAsync(u => u.IsActive);
        public Task<int> CountInactiveUsersAsync() => _context.Users.CountAsync(u => !u.IsActive);
        public Task<int> CountUsersByRoleAsync(int roleId) => _context.Users.CountAsync(u => u.Role == roleId);
        public Task<int> CountRecentUsersAsync(int days) => _context.Users.CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-days));
    }
}


