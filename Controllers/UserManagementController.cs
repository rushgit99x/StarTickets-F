using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Filters;
using StarTickets.Models;
using StarTickets.Models.ViewModels;
using StarTickets.Services;
using System.Security.Cryptography;

namespace StarTickets.Controllers
{
    [RoleAuthorize("1")] // Only Admin can access
    public class UserManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserManagementController> _logger;
        private readonly IEmailService _emailService;

        public UserManagementController(
            ApplicationDbContext context,
            ILogger<UserManagementController> logger,
            IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        // GET: UserManagement/Index
        public async Task<IActionResult> Index(string search = "", int page = 1, int pageSize = 10, int? roleFilter = null, bool? activeFilter = null)
        {
            var query = _context.Users.Include(u => u.UserRole).AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    u.FirstName.Contains(search) ||
                    u.LastName.Contains(search) ||
                    u.Email.Contains(search));
            }

            // Apply role filter
            if (roleFilter.HasValue)
            {
                query = query.Where(u => u.Role == roleFilter.Value);
            }

            // Apply active status filter
            if (activeFilter.HasValue)
            {
                query = query.Where(u => u.IsActive == activeFilter.Value);
            }

            // Calculate pagination
            var totalUsers = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            var users = await query
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new UserManagementViewModel
            {
                Users = users,
                SearchTerm = search,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalUsers = totalUsers,
                RoleFilter = roleFilter,
                ActiveFilter = activeFilter,
                UserRoles = await _context.UserRoles.ToListAsync()
            };

            return View(viewModel);
        }

        // GET: UserManagement/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new CreateUserViewModel
            {
                UserRoles = await _context.UserRoles.ToListAsync()
            };
            return View(viewModel);
        }

        // POST: UserManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email address is already in use");
                    model.UserRoles = await _context.UserRoles.ToListAsync();
                    return View(model);
                }

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
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"User created by admin: {user.Email}");

                    // Send welcome email to the newly created user
                    try
                    {
                        await _emailService.SendWelcomeEmailAsync(user);
                        _logger.LogInformation($"Welcome email sent successfully to: {user.Email}");
                        TempData["SuccessMessage"] = $"User {user.FullName} created successfully and welcome email has been sent.";
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, $"Failed to send welcome email to: {user.Email}");
                        // Don't fail the user creation if email fails
                        TempData["SuccessMessage"] = $"User {user.FullName} created successfully, but welcome email could not be sent.";
                        TempData["WarningMessage"] = "The welcome email could not be sent due to email service issues.";
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error creating user: {model.Email}");
                    ModelState.AddModelError("", "An error occurred while creating the user. Please try again.");
                }
            }

            model.UserRoles = await _context.UserRoles.ToListAsync();
            return View(model);
        }

        // GET: UserManagement/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new EditUserViewModel
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
                UserRoles = await _context.UserRoles.ToListAsync()
            };

            return View(viewModel);
        }

        // POST: UserManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(model.UserId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if email is changed and already exists
                if (user.Email != model.Email && await _context.Users.AnyAsync(u => u.Email == model.Email && u.UserId != model.UserId))
                {
                    ModelState.AddModelError("Email", "Email address is already in use");
                    model.UserRoles = await _context.UserRoles.ToListAsync();
                    return View(model);
                }

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

                    // Update password if provided
                    if (!string.IsNullOrWhiteSpace(model.NewPassword))
                    {
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                    }

                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"User updated by admin: {user.Email}");
                    TempData["SuccessMessage"] = $"User {user.FullName} updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating user: {model.Email}");
                    ModelState.AddModelError("", "An error occurred while updating the user. Please try again.");
                }
            }

            model.UserRoles = await _context.UserRoles.ToListAsync();
            return View(model);
        }

        // GET: UserManagement/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserRole)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            // Get user's booking statistics
            var bookingStats = await GetUserBookingStats(id);

            var viewModel = new UserDetailsViewModel
            {
                User = user,
                BookingStats = bookingStats
            };

            return View(viewModel);
        }

        // POST: UserManagement/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            try
            {
                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.UtcNow;

                _context.Update(user);
                await _context.SaveChangesAsync();

                var status = user.IsActive ? "activated" : "deactivated";
                _logger.LogInformation($"User {status} by admin: {user.Email}");

                return Json(new
                {
                    success = true,
                    message = $"User {user.FullName} has been {status}.",
                    isActive = user.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling user status: {user.Email}");
                return Json(new { success = false, message = "An error occurred while updating the user status." });
            }
        }

        // POST: UserManagement/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                return Json(new { success = false, message = "Password must be at least 6 characters long." });
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            try
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.ResetToken = null;
                user.ResetTokenExpiry = null;
                user.UpdatedAt = DateTime.UtcNow;

                _context.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Password reset by admin for user: {user.Email}");

                return Json(new
                {
                    success = true,
                    message = $"Password has been reset for {user.FullName}."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resetting password for user: {user.Email}");
                return Json(new { success = false, message = "An error occurred while resetting the password." });
            }
        }

        // POST: UserManagement/ResendWelcomeEmail/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendWelcomeEmail(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            try
            {
                await _emailService.SendWelcomeEmailAsync(user);
                _logger.LogInformation($"Welcome email resent to user: {user.Email}");

                return Json(new
                {
                    success = true,
                    message = $"Welcome email has been sent to {user.FullName}."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resending welcome email to user: {user.Email}");
                return Json(new { success = false, message = "An error occurred while sending the welcome email." });
            }
        }

        // GET: UserManagement/GetUserStats
        public async Task<IActionResult> GetUserStats()
        {
            try
            {
                var stats = new
                {
                    TotalUsers = await _context.Users.CountAsync(),
                    ActiveUsers = await _context.Users.CountAsync(u => u.IsActive),
                    InactiveUsers = await _context.Users.CountAsync(u => !u.IsActive),
                    AdminUsers = await _context.Users.CountAsync(u => u.Role == 1),
                    OrganizerUsers = await _context.Users.CountAsync(u => u.Role == 2),
                    CustomerUsers = await _context.Users.CountAsync(u => u.Role == 3),
                    RecentUsers = await _context.Users.CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                };

                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user statistics");
                return Json(new { success = false, message = "Error loading statistics" });
            }
        }

        // POST: UserManagement/BulkAction
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAction(string action, int[] userIds)
        {
            if (userIds == null || userIds.Length == 0)
            {
                return Json(new { success = false, message = "No users selected." });
            }

            try
            {
                var users = await _context.Users.Where(u => userIds.Contains(u.UserId)).ToListAsync();

                switch (action.ToLower())
                {
                    case "activate":
                        foreach (var user in users)
                        {
                            user.IsActive = true;
                            user.UpdatedAt = DateTime.UtcNow;
                        }
                        _context.UpdateRange(users);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Bulk activated {users.Count} users");
                        return Json(new { success = true, message = $"{users.Count} users activated successfully." });

                    case "deactivate":
                        foreach (var user in users)
                        {
                            user.IsActive = false;
                            user.UpdatedAt = DateTime.UtcNow;
                        }
                        _context.UpdateRange(users);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Bulk deactivated {users.Count} users");
                        return Json(new { success = true, message = $"{users.Count} users deactivated successfully." });

                    case "confirm_email":
                        foreach (var user in users)
                        {
                            user.EmailConfirmed = true;
                            user.UpdatedAt = DateTime.UtcNow;
                        }
                        _context.UpdateRange(users);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Bulk confirmed email for {users.Count} users");
                        return Json(new { success = true, message = $"Email confirmed for {users.Count} users." });

                    case "send_welcome_email":
                        var emailsSent = 0;
                        var emailsFailed = 0;

                        foreach (var user in users)
                        {
                            try
                            {
                                await _emailService.SendWelcomeEmailAsync(user);
                                emailsSent++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Failed to send welcome email to: {user.Email}");
                                emailsFailed++;
                            }
                        }

                        var message = $"Welcome emails sent to {emailsSent} users.";
                        if (emailsFailed > 0)
                        {
                            message += $" {emailsFailed} emails failed to send.";
                        }

                        _logger.LogInformation($"Bulk welcome emails: {emailsSent} sent, {emailsFailed} failed");
                        return Json(new { success = true, message = message });

                    case "delete":
                        // Check for dependencies before bulk delete
                        var usersWithBookings = await _context.Bookings
                            .Where(b => userIds.Contains(b.CustomerId))
                            .Select(b => b.CustomerId)
                            .Distinct()
                            .ToListAsync();

                        var usersWithEvents = await _context.Events
                            .Where(e => userIds.Contains(e.OrganizerId))
                            .Select(e => e.OrganizerId)
                            .Distinct()
                            .ToListAsync();

                        var usersWithDependencies = usersWithBookings.Union(usersWithEvents).ToList();

                        if (usersWithDependencies.Any())
                        {
                            return Json(new
                            {
                                success = false,
                                message = $"Cannot delete {usersWithDependencies.Count} user(s) with existing bookings or events."
                            });
                        }

                        _context.Users.RemoveRange(users);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Bulk deleted {users.Count} users");
                        return Json(new { success = true, message = $"{users.Count} users deleted successfully." });

                    default:
                        return Json(new { success = false, message = "Invalid action." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error performing bulk action: {action}");
                return Json(new { success = false, message = "An error occurred while performing the bulk action." });
            }
        }

        // GET: UserManagement/Export
        public async Task<IActionResult> Export(string format = "csv")
        {
            try
            {
                var users = await _context.Users
                    .Include(u => u.UserRole)
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .ToListAsync();

                if (format.ToLower() == "csv")
                {
                    var csv = GenerateUsersCsv(users);
                    return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"users_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
                }

                return BadRequest("Unsupported export format");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting users");
                TempData["ErrorMessage"] = "An error occurred while exporting users.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper Methods
        private async Task<UserBookingStats> GetUserBookingStats(int userId)
        {
            // Since we don't have booking data yet, return empty stats
            // This will be populated when booking functionality is implemented
            return new UserBookingStats
            {
                TotalBookings = 0,
                TotalSpent = 0,
                LastBookingDate = null,
                FavoriteEventCategory = "N/A"
            };
        }

        private string GenerateUsersCsv(List<User> users)
        {
            var csv = new System.Text.StringBuilder();

            // Header
            csv.AppendLine("ID,Email,First Name,Last Name,Phone,Role,Active,Email Confirmed,Loyalty Points,Created At");

            // Data rows
            foreach (var user in users)
            {
                csv.AppendLine($"{user.UserId},{user.Email},{user.FirstName},{user.LastName},{user.PhoneNumber},{user.UserRole?.RoleName},{user.IsActive},{user.EmailConfirmed},{user.LoyaltyPoints},{user.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }

            return csv.ToString();
        }

        // GET: UserManagement/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users
                .Include(u => u.UserRole)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check if user has any dependencies that would prevent deletion
            var hasBookings = await _context.Bookings.AnyAsync(b => b.CustomerId == id);
            var hasEvents = await _context.Events.AnyAsync(e => e.OrganizerId == id);

            var viewModel = new DeleteUserViewModel
            {
                User = user,
                HasBookings = hasBookings,
                HasEvents = hasEvents,
                CanDelete = !hasBookings && !hasEvents // Only allow deletion if no dependencies
            };

            return View(viewModel);
        }

        // POST: UserManagement/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, bool forceDelete = false)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            try
            {
                // Check for dependencies
                var hasBookings = await _context.Bookings.AnyAsync(b => b.CustomerId == id);
                var hasEvents = await _context.Events.AnyAsync(e => e.OrganizerId == id);

                if ((hasBookings || hasEvents) && !forceDelete)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Cannot delete user with existing bookings or events. Use force delete if necessary.",
                        hasDependencies = true
                    });
                }

                // For safety, don't implement force delete for users with dependencies
                if (forceDelete && (hasBookings || hasEvents))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Force delete is not implemented for users with dependencies."
                    });
                }

                // Perform the deletion
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User deleted by admin: {user.Email} (ID: {user.UserId})");

                return Json(new
                {
                    success = true,
                    message = $"User {user.FullName} has been successfully deleted."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user: {user.Email}");
                return Json(new
                {
                    success = false,
                    message = "An error occurred while deleting the user. Please try again."
                });
            }
        }
    }
}