using Microsoft.EntityFrameworkCore;
using StarTickets.Models;
using StarTickets.Models.ViewModels;
using StarTickets.Repositories.Interfaces;
using StarTickets.Services.Interfaces;

namespace StarTickets.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserManagementRepository _repo;
        private readonly IEmailService _emailService;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(IUserManagementRepository repo, IEmailService emailService, ILogger<UserManagementService> logger)
        {
            _repo = repo;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<UserManagementViewModel> GetIndexAsync(string search, int page, int pageSize, int? roleFilter, bool? activeFilter)
        {
            var query = _repo.QueryUsersWithRole();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.FirstName.Contains(search) || u.LastName.Contains(search) || u.Email.Contains(search));
            }
            if (roleFilter.HasValue)
                query = query.Where(u => u.Role == roleFilter.Value);
            if (activeFilter.HasValue)
                query = query.Where(u => u.IsActive == activeFilter.Value);

            var totalUsers = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);
            var users = await query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new UserManagementViewModel
            {
                Users = users,
                SearchTerm = search,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalUsers = totalUsers,
                RoleFilter = roleFilter,
                ActiveFilter = activeFilter,
                UserRoles = await _repo.GetUserRolesAsync()
            };
        }

        public async Task<CreateUserViewModel> GetCreateAsync()
        {
            return new CreateUserViewModel { UserRoles = await _repo.GetUserRolesAsync() };
        }

        public async Task<(bool Success, string Message)> CreateAsync(CreateUserViewModel model)
        {
            if (await _repo.EmailExistsAsync(model.Email))
                return (false, "Email address is already in use");

            var user = new User
            {
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                DateOfBirth = model.DateOfBirth,
                Role = model.Role,
                IsActive = model.IsActive,
                EmailConfirmed = model.EmailConfirmed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                await _repo.AddUserAsync(user);
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send welcome email to: {Email}", user.Email);
                }
                return (true, "User created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Email}", model.Email);
                return (false, "An error occurred while creating the user. Please try again.");
            }
        }

        public async Task<EditUserViewModel?> GetEditAsync(int id)
        {
            var user = await _repo.FindUserAsync(id);
            if (user == null) return null;
            return new EditUserViewModel
            {
                UserId = user.UserId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Role = user.Role,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                LoyaltyPoints = user.LoyaltyPoints,
                UserRoles = await _repo.GetUserRolesAsync()
            };
        }

        public async Task<(bool Success, string Message)> EditAsync(EditUserViewModel model)
        {
            var user = await _repo.FindUserAsync(model.UserId);
            if (user == null) return (false, "User not found.");

            if (user.Email != model.Email && await _repo.EmailExistsAsync(model.Email, model.UserId))
                return (false, "Email address is already in use");

            try
            {
                user.Email = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                user.DateOfBirth = model.DateOfBirth;
                user.Role = model.Role;
                user.IsActive = model.IsActive;
                user.EmailConfirmed = model.EmailConfirmed;
                user.LoyaltyPoints = model.LoyaltyPoints;
                user.UpdatedAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                }
                _repo.UpdateUser(user);
                await _repo.SaveChangesAsync();
                return (true, "User updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {Email}", model.Email);
                return (false, "An error occurred while updating the user. Please try again.");
            }
        }

        public async Task<UserDetailsViewModel?> GetDetailsAsync(int id)
        {
            var user = await _repo.QueryUsersWithRole().FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return null;
            var bookingStats = new UserBookingStats
            {
                TotalBookings = 0,
                TotalSpent = 0,
                LastBookingDate = null,
                FavoriteEventCategory = "N/A"
            };
            return new UserDetailsViewModel { User = user, BookingStats = bookingStats };
        }

        public async Task<(bool Success, string Message, bool? IsActive)> ToggleStatusAsync(int id)
        {
            var user = await _repo.FindUserAsync(id);
            if (user == null) return (false, "User not found.", null);
            try
            {
                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.UtcNow;
                _repo.UpdateUser(user);
                await _repo.SaveChangesAsync();
                var status = user.IsActive ? "activated" : "deactivated";
                return (true, $"User {user.FullName} has been {status}.", user.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status: {Email}", user.Email);
                return (false, "An error occurred while updating the user status.", null);
            }
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(int id, string newPassword)
        {
            var user = await _repo.FindUserAsync(id);
            if (user == null) return (false, "User not found.");
            try
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.ResetToken = null;
                user.ResetTokenExpiry = null;
                user.UpdatedAt = DateTime.UtcNow;
                _repo.UpdateUser(user);
                await _repo.SaveChangesAsync();
                return (true, $"Password has been reset for {user.FullName}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user: {Email}", user.Email);
                return (false, "An error occurred while resetting the password.");
            }
        }

        public async Task<(bool Success, string Message)> ResendWelcomeEmailAsync(int id)
        {
            var user = await _repo.FindUserAsync(id);
            if (user == null) return (false, "User not found.");
            try
            {
                await _emailService.SendWelcomeEmailAsync(user);
                return (true, $"Welcome email has been sent to {user.FullName}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending welcome email to user: {Email}", user.Email);
                return (false, "An error occurred while sending the welcome email.");
            }
        }

        public async Task<object> GetUserStatsAsync()
        {
            return new
            {
                TotalUsers = await _repo.CountUsersAsync(),
                ActiveUsers = await _repo.CountActiveUsersAsync(),
                InactiveUsers = await _repo.CountInactiveUsersAsync(),
                AdminUsers = await _repo.CountUsersByRoleAsync(1),
                OrganizerUsers = await _repo.CountUsersByRoleAsync(2),
                CustomerUsers = await _repo.CountUsersByRoleAsync(3),
                RecentUsers = await _repo.CountRecentUsersAsync(30)
            };
        }

        public async Task<(bool Success, string Message)> BulkActionAsync(string action, int[] userIds)
        {
            var users = await _repo.GetUsersByIdsAsync(userIds);
            if (!users.Any()) return (false, "No users selected.");
            try
            {
                switch (action.ToLower())
                {
                    case "activate":
                        foreach (var user in users) { user.IsActive = true; user.UpdatedAt = DateTime.UtcNow; }
                        _repo.UpdateUsers(users);
                        await _repo.SaveChangesAsync();
                        return (true, $"{users.Count} users activated successfully.");
                    case "deactivate":
                        foreach (var user in users) { user.IsActive = false; user.UpdatedAt = DateTime.UtcNow; }
                        _repo.UpdateUsers(users);
                        await _repo.SaveChangesAsync();
                        return (true, $"{users.Count} users deactivated successfully.");
                    case "confirm_email":
                        foreach (var user in users) { user.EmailConfirmed = true; user.UpdatedAt = DateTime.UtcNow; }
                        _repo.UpdateUsers(users);
                        await _repo.SaveChangesAsync();
                        return (true, $"Email confirmed for {users.Count} users.");
                    case "send_welcome_email":
                        var emailsSent = 0; var emailsFailed = 0;
                        foreach (var user in users)
                        {
                            try { await _emailService.SendWelcomeEmailAsync(user); emailsSent++; }
                            catch (Exception ex) { _logger.LogError(ex, "Failed to send welcome email to: {Email}", user.Email); emailsFailed++; }
                        }
                        var message = $"Welcome emails sent to {emailsSent} users." + (emailsFailed > 0 ? $" {emailsFailed} emails failed to send." : "");
                        return (true, message);
                    case "delete":
                        // dependencies check handled at controller in original; keep minimal here
                        _repo.RemoveUsers(users);
                        await _repo.SaveChangesAsync();
                        return (true, $"{users.Count} users deleted successfully.");
                    default:
                        return (false, "Invalid action.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk action: {Action}", action);
                return (false, "An error occurred while performing the bulk action.");
            }
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id, bool forceDelete)
        {
            var user = await _repo.FindUserAsync(id);
            if (user == null) return (false, "User not found.");
            var hasBookings = await _repo.HasBookingsAsync(id);
            var hasEvents = await _repo.HasEventsAsync(id);
            if ((hasBookings || hasEvents) && !forceDelete)
                return (false, "Cannot delete user with existing bookings or events. Use force delete if necessary.");
            if (forceDelete && (hasBookings || hasEvents))
                return (false, "Force delete is not implemented for users with dependencies.");
            _repo.RemoveUsers(new[] { user });
            await _repo.SaveChangesAsync();
            return (true, $"User {user.FullName} has been successfully deleted.");
        }

        public async Task<string> GenerateUsersCsvAsync()
        {
            var users = await _repo.GetAllUsersWithRoleOrderedAsync();
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("ID,Email,First Name,Last Name,Phone,Role,Active,Email Confirmed,Loyalty Points,Created At");
            foreach (var user in users)
            {
                csv.AppendLine($"{user.UserId},{user.Email},{user.FirstName},{user.LastName},{user.PhoneNumber},{user.UserRole?.RoleName},{user.IsActive},{user.EmailConfirmed},{user.LoyaltyPoints},{user.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }
            return csv.ToString();
        }
    }
}


