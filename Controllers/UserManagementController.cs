using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Filters;
using StarTickets.Models;
using StarTickets.Models.ViewModels;
using StarTickets.Services;
using StarTickets.Services.Interfaces;
using System.Net.Mail;
using System.Security.Cryptography;

namespace StarTickets.Controllers
{
    [RoleAuthorize("1")] // Only Admin can access
    public class UserManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserManagementController> _logger;
        private readonly IEmailService _emailService;
        private readonly IUserManagementService _service;

        public UserManagementController(
            ApplicationDbContext context,
            ILogger<UserManagementController> logger,
            IEmailService emailService,
            IUserManagementService service)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        // GET: UserManagement/Index
        public async Task<IActionResult> Index(string search = "", int page = 1, int pageSize = 10, int? roleFilter = null, bool? activeFilter = null)
        {
            try
            {
                // Validate pagination parameters
                if (page < 1)
                {
                    _logger.LogWarning($"Invalid page number: {page}. Resetting to 1.");
                    page = 1;
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    _logger.LogWarning($"Invalid page size: {pageSize}. Resetting to 10.");
                    pageSize = 10;
                }

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
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while loading user management index");
                TempData["ErrorMessage"] = "A database error occurred. Please try again later.";
                return View(new UserManagementViewModel { Users = new List<User>() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while loading user management index");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please contact support if the problem persists.";
                return View(new UserManagementViewModel { Users = new List<User>() });
            }
        }

        // GET: UserManagement/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var viewModel = new CreateUserViewModel
                {
                    UserRoles = await _context.UserRoles.ToListAsync()
                };
                return View(viewModel);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error occurred while loading create user page");
                TempData["ErrorMessage"] = "Unable to load user creation form. Please try again.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create user page");
                TempData["ErrorMessage"] = "An error occurred. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: UserManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            try
            {
                if (model == null)
                {
                    throw new ArgumentNullException(nameof(model), "User model cannot be null");
                }

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
                    catch (SmtpException smtpEx)
                    {
                        _logger.LogError(smtpEx, $"SMTP error while sending welcome email to: {user.Email}");
                        TempData["SuccessMessage"] = $"User {user.FullName} created successfully, but welcome email could not be sent.";
                        TempData["WarningMessage"] = "The welcome email could not be sent due to email server issues.";
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, $"Failed to send welcome email to: {user.Email}");
                        TempData["SuccessMessage"] = $"User {user.FullName} created successfully, but welcome email could not be sent.";
                        TempData["WarningMessage"] = "The welcome email could not be sent due to email service issues.";
                    }

                    return RedirectToAction(nameof(Index));
                }

                model.UserRoles = await _context.UserRoles.ToListAsync();
                return View(model);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error creating user: {model?.Email}");
                ModelState.AddModelError("", "A database error occurred. The email might already be in use or there's a constraint violation.");
                model.UserRoles = await _context.UserRoles.ToListAsync();
                return View(model);
            }
            catch (CryptographicException cryptoEx)
            {
                _logger.LogError(cryptoEx, $"Password hashing error for user: {model?.Email}");
                ModelState.AddModelError("", "An error occurred while securing the password. Please try again.");
                model.UserRoles = await _context.UserRoles.ToListAsync();
                return View(model);
            }
            catch (ArgumentNullException argEx)
            {
                _logger.LogError(argEx, "Null argument error during user creation");
                ModelState.AddModelError("", "Invalid data provided. Please check all required fields.");
                model.UserRoles = await _context.UserRoles.ToListAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error creating user: {model?.Email}");
                ModelState.AddModelError("", "An unexpected error occurred while creating the user. Please try again.");
                model.UserRoles = await _context.UserRoles.ToListAsync();
                return View(model);
            }
        }

        // GET: UserManagement/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning($"Invalid user ID provided for edit: {id}");
                    TempData["ErrorMessage"] = "Invalid user ID.";
                    return RedirectToAction(nameof(Index));
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for edit: ID {id}");
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
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error loading user for edit: ID {id}");
                TempData["ErrorMessage"] = "A database error occurred while loading the user.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading user for edit: ID {id}");
                TempData["ErrorMessage"] = "An error occurred while loading the user details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: UserManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            try
            {
                if (model == null)
                {
                    throw new ArgumentNullException(nameof(model), "Edit model cannot be null");
                }

                if (ModelState.IsValid)
                {
                    var user = await _context.Users.FindAsync(model.UserId);
                    if (user == null)
                    {
                        _logger.LogWarning($"User not found for edit: ID {model.UserId}");
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

                model.UserRoles = await _context.UserRoles.ToListAsync();
                return View(model);
            }
            catch (DbUpdateConcurrencyException concEx)
            {
                _logger.LogError(concEx, $"Concurrency error updating user: {model?.Email}");
                ModelState.AddModelError("", "The user was modified by another user. Please reload and try again.");
                model.UserRoles = await _context.UserRoles.ToListAsync();
                return View(model);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error updating user: {model?.Email}");
                ModelState.AddModelError("", "A database error occurred. Please check the data and try again.");
                model.UserRoles = await _context.UserRoles.ToListAsync();
                return View(model);
            }
            catch (CryptographicException cryptoEx)
            {
                _logger.LogError(cryptoEx, $"Password hashing error for user: {model?.Email}");
                ModelState.AddModelError("", "An error occurred while securing the new password. Please try again.");
                model.UserRoles = await _context.UserRoles.ToListAsync();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error updating user: {model?.Email}");
                ModelState.AddModelError("", "An unexpected error occurred while updating the user. Please try again.");
                model.UserRoles = await _context.UserRoles.ToListAsync();
                return View(model);
            }
        }

        // GET: UserManagement/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning($"Invalid user ID provided for details: {id}");
                    TempData["ErrorMessage"] = "Invalid user ID.";
                    return RedirectToAction(nameof(Index));
                }

                var user = await _context.Users
                    .Include(u => u.UserRole)
                    .FirstOrDefaultAsync(u => u.UserId == id);

                if (user == null)
                {
                    _logger.LogWarning($"User not found for details: ID {id}");
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
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error loading user details: ID {id}");
                TempData["ErrorMessage"] = "A database error occurred while loading user details.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading user details: ID {id}");
                TempData["ErrorMessage"] = "An error occurred while loading user details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: UserManagement/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning($"Invalid user ID for status toggle: {id}");
                    return Json(new { success = false, message = "Invalid user ID." });
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for status toggle: ID {id}");
                    return Json(new { success = false, message = "User not found." });
                }

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
            catch (DbUpdateConcurrencyException concEx)
            {
                _logger.LogError(concEx, $"Concurrency error toggling user status: ID {id}");
                return Json(new { success = false, message = "The user was modified by another user. Please refresh and try again." });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error toggling user status: ID {id}");
                return Json(new { success = false, message = "A database error occurred while updating the user status." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling user status: ID {id}");
                return Json(new { success = false, message = "An unexpected error occurred while updating the user status." });
            }
        }

        // POST: UserManagement/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning($"Invalid user ID for password reset: {id}");
                    return Json(new { success = false, message = "Invalid user ID." });
                }

                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                {
                    return Json(new { success = false, message = "Password must be at least 6 characters long." });
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for password reset: ID {id}");
                    return Json(new { success = false, message = "User not found." });
                }

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
            catch (CryptographicException cryptoEx)
            {
                _logger.LogError(cryptoEx, $"Password hashing error during reset for user ID: {id}");
                return Json(new { success = false, message = "An error occurred while securing the password." });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error resetting password for user ID: {id}");
                return Json(new { success = false, message = "A database error occurred while resetting the password." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resetting password for user ID: {id}");
                return Json(new { success = false, message = "An unexpected error occurred while resetting the password." });
            }
        }

        // POST: UserManagement/ResendWelcomeEmail/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendWelcomeEmail(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning($"Invalid user ID for resending welcome email: {id}");
                    return Json(new { success = false, message = "Invalid user ID." });
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for resending welcome email: ID {id}");
                    return Json(new { success = false, message = "User not found." });
                }

                await _emailService.SendWelcomeEmailAsync(user);
                _logger.LogInformation($"Welcome email resent to user: {user.Email}");

                return Json(new
                {
                    success = true,
                    message = $"Welcome email has been sent to {user.FullName}."
                });
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, $"SMTP error resending welcome email to user ID: {id}");
                return Json(new { success = false, message = "Email server error. Please try again later." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resending welcome email to user ID: {id}");
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
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error getting user statistics");
                return Json(new { success = false, message = "Database error loading statistics" });
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
            try
            {
                if (string.IsNullOrWhiteSpace(action))
                {
                    _logger.LogWarning("Bulk action called with null or empty action");
                    return Json(new { success = false, message = "Invalid action specified." });
                }

                if (userIds == null || userIds.Length == 0)
                {
                    return Json(new { success = false, message = "No users selected." });
                }

                if (userIds.Any(id => id <= 0))
                {
                    _logger.LogWarning("Bulk action called with invalid user IDs");
                    return Json(new { success = false, message = "Invalid user IDs detected." });
                }

                var users = await _context.Users.Where(u => userIds.Contains(u.UserId)).ToListAsync();

                if (users.Count != userIds.Length)
                {
                    _logger.LogWarning($"Bulk action: {userIds.Length - users.Count} user(s) not found");
                }

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
                            catch (SmtpException smtpEx)
                            {
                                _logger.LogError(smtpEx, $"SMTP error sending welcome email to: {user.Email}");
                                emailsFailed++;
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
                        _logger.LogWarning($"Invalid bulk action requested: {action}");
                        return Json(new { success = false, message = "Invalid action." });
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error performing bulk action: {action}");
                return Json(new { success = false, message = "A database error occurred during the bulk operation." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error performing bulk action: {action}");
                return Json(new { success = false, message = "An unexpected error occurred during the bulk operation." });
            }
        }

        // GET: UserManagement/Export
        public async Task<IActionResult> Export(string format = "csv")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(format))
                {
                    format = "csv";
                }

                if (format.ToLower() != "csv")
                {
                    _logger.LogWarning($"Unsupported export format requested: {format}");
                    TempData["ErrorMessage"] = "Unsupported export format. Only CSV is supported.";
                    return RedirectToAction(nameof(Index));
                }

                var users = await _context.Users
                    .Include(u => u.UserRole)
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .ToListAsync();

                var csv = GenerateUsersCsv(users);
                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                var fileName = $"users_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

                _logger.LogInformation($"Exported {users.Count} users to CSV");

                return File(bytes, "text/csv", fileName);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error exporting users");
                TempData["ErrorMessage"] = "A database error occurred while exporting users.";
                return RedirectToAction(nameof(Index));
            }
            catch (OutOfMemoryException memEx)
            {
                _logger.LogError(memEx, "Out of memory error during export");
                TempData["ErrorMessage"] = "Too many users to export at once. Please filter the data and try again.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting users");
                TempData["ErrorMessage"] = "An error occurred while exporting users.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: UserManagement/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning($"Invalid user ID for delete: {id}");
                    TempData["ErrorMessage"] = "Invalid user ID.";
                    return RedirectToAction(nameof(Index));
                }

                var user = await _context.Users
                    .Include(u => u.UserRole)
                    .FirstOrDefaultAsync(u => u.UserId == id);

                if (user == null)
                {
                    _logger.LogWarning($"User not found for delete: ID {id}");
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
                    CanDelete = !hasBookings && !hasEvents
                };

                return View(viewModel);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error loading delete page for user ID: {id}");
                TempData["ErrorMessage"] = "A database error occurred while loading the delete page.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading delete page for user ID: {id}");
                TempData["ErrorMessage"] = "An error occurred while loading the delete page.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: UserManagement/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, bool forceDelete = false)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning($"Invalid user ID for delete confirmation: {id}");
                    return Json(new { success = false, message = "Invalid user ID." });
                }

                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for delete confirmation: ID {id}");
                    return Json(new { success = false, message = "User not found." });
                }

                // Check for dependencies
                var hasBookings = await _context.Bookings.AnyAsync(b => b.CustomerId == id);
                var hasEvents = await _context.Events.AnyAsync(e => e.OrganizerId == id);

                if ((hasBookings || hasEvents) && !forceDelete)
                {
                    _logger.LogWarning($"Attempted to delete user with dependencies: ID {id}");
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
                    _logger.LogWarning($"Force delete attempted for user with dependencies: ID {id}");
                    return Json(new
                    {
                        success = false,
                        message = "Force delete is not implemented for users with dependencies."
                    });
                }

                // Store user info for logging before deletion
                var userEmail = user.Email;
                var userId = user.UserId;
                var userFullName = user.FullName;

                // Perform the deletion
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User deleted by admin: {userEmail} (ID: {userId})");

                return Json(new
                {
                    success = true,
                    message = $"User {userFullName} has been successfully deleted."
                });
            }
            catch (DbUpdateConcurrencyException concEx)
            {
                _logger.LogError(concEx, $"Concurrency error deleting user ID: {id}");
                return Json(new
                {
                    success = false,
                    message = "The user was modified by another user. Please refresh and try again."
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error deleting user ID: {id}");
                return Json(new
                {
                    success = false,
                    message = "A database error occurred while deleting the user. The user may have dependencies."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user ID: {id}");
                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred while deleting the user. Please try again."
                });
            }
        }

        // Helper Methods
        private async Task<UserBookingStats> GetUserBookingStats(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    _logger.LogWarning($"Invalid user ID for booking stats: {userId}");
                    return new UserBookingStats
                    {
                        TotalBookings = 0,
                        TotalSpent = 0,
                        LastBookingDate = null,
                        FavoriteEventCategory = "N/A"
                    };
                }

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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting booking stats for user ID: {userId}");
                // Return default stats on error
                return new UserBookingStats
                {
                    TotalBookings = 0,
                    TotalSpent = 0,
                    LastBookingDate = null,
                    FavoriteEventCategory = "Error loading stats"
                };
            }
        }

        private string GenerateUsersCsv(List<User> users)
        {
            try
            {
                if (users == null)
                {
                    throw new ArgumentNullException(nameof(users), "Users list cannot be null");
                }

                var csv = new System.Text.StringBuilder();

                // Header
                csv.AppendLine("ID,Email,First Name,Last Name,Phone,Role,Active,Email Confirmed,Loyalty Points,Created At");

                // Data rows
                foreach (var user in users)
                {
                    if (user == null)
                    {
                        _logger.LogWarning("Null user encountered during CSV generation");
                        continue;
                    }

                    // Escape fields that might contain commas or quotes
                    var email = EscapeCsvField(user.Email);
                    var firstName = EscapeCsvField(user.FirstName);
                    var lastName = EscapeCsvField(user.LastName);
                    var phone = EscapeCsvField(user.PhoneNumber);
                    var roleName = EscapeCsvField(user.UserRole?.RoleName);

                    csv.AppendLine($"{user.UserId},{email},{firstName},{lastName},{phone},{roleName},{user.IsActive},{user.EmailConfirmed},{user.LoyaltyPoints},{user.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                }

                return csv.ToString();
            }
            catch (ArgumentNullException argEx)
            {
                _logger.LogError(argEx, "Null argument in CSV generation");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating CSV");
                throw new InvalidOperationException("Failed to generate CSV export", ex);
            }
        }

        private string EscapeCsvField(string field)
        {
            try
            {
                if (string.IsNullOrEmpty(field))
                {
                    return string.Empty;
                }

                // If field contains comma, quote, or newline, wrap it in quotes and escape internal quotes
                if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
                {
                    return $"\"{field.Replace("\"", "\"\"")}\"";
                }

                return field;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error escaping CSV field: {field}");
                return string.Empty;
            }
        }
    }
}