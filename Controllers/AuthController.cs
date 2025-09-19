using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Models.ViewModels;
using StarTickets.Models;
using StarTickets.Services;
using System.Security.Cryptography;

namespace StarTickets.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        // GET: /Account/Register
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("", "Email already exists");
                    return View(model);
                }

                var user = new User
                {
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Role = model.Role, // Customer=3 or EventOrganizer=2
                    IsActive = true
                };

                try
                {
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Send welcome email
                    await _emailService.SendWelcomeEmailAsync(user);
                    _logger.LogInformation($"User registered successfully: {user.Email}");

                    // Add success message
                    TempData["SuccessMessage"] = "Registration successful! Please check your email for a welcome message.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error during user registration: {model.Email}");
                    ModelState.AddModelError("", "An error occurred during registration. Please try again.");
                    return View(model);
                }
            }
            return View(model);
        }

        // GET: /Account/Login
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    // Save session (simple auth)
                    HttpContext.Session.SetInt32("UserId", user.UserId);
                    HttpContext.Session.SetString("Role", user.Role.ToString());

                    // Redirect by role
                    return user.Role switch
                    {
                        1 => RedirectToAction("Index", "Admin"),
                        2 => RedirectToAction("Index", "EventOrganizer"),
                        //3 => RedirectToAction("Index", "Customer"),
                        3 => RedirectToAction("Index", "Home"),
                        _ => RedirectToAction("Login")
                    };
                }
                ModelState.AddModelError("", "Invalid login attempt");
            }
            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // GET: /Account/ForgotPassword
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

                if (user != null && user.IsActive)
                {
                    // Generate password reset token
                    var token = GeneratePasswordResetToken();
                    var tokenExpiry = DateTime.UtcNow.AddMinutes(10); // Token expires in 10 minutes

                    // Update user with reset token
                    user.ResetToken = token;
                    user.ResetTokenExpiry = tokenExpiry;
                    user.UpdatedAt = DateTime.UtcNow;

                    try
                    {
                        await _context.SaveChangesAsync();

                        // Generate password reset email
                        var resetUrl = Url.Action("ResetPassword", "Auth",
                            new { token = token, email = model.Email }, Request.Scheme);

                        await _emailService.SendPasswordResetEmailAsync(user, resetUrl);

                        _logger.LogInformation($"Password reset token generated for user: {user.Email}");

                        TempData["SuccessMessage"] = "If the email address exists in our system, you will receive a password reset link shortly.";
                        return RedirectToAction("Login");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error sending password reset email: {model.Email}");
                        ModelState.AddModelError("", "An error occurred while processing your request. Please try again.");
                        return View(model);
                    }
                }
                else
                {
                    // Show explicit error when email is not registered or user is inactive
                    ModelState.AddModelError("Email", "Email not found");
                    return View(model);
                }
            }
            return View(model);
        }

        // GET: /Account/ResetPassword
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Invalid password reset link.";
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == email &&
                u.ResetToken == token &&
                u.ResetTokenExpiry > DateTime.UtcNow);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid or expired password reset link.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.Email == model.Email &&
                    u.ResetToken == model.Token &&
                    u.ResetTokenExpiry > DateTime.UtcNow);

                if (user == null)
                {
                    TempData["ErrorMessage"] = "Invalid or expired password reset link.";
                    return RedirectToAction("Login");
                }

                try
                {
                    // Update password and clear reset token
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    user.ResetToken = null;
                    user.ResetTokenExpiry = null;
                    user.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    // Send password reset confirmation email
                    await _emailService.SendPasswordResetConfirmationEmailAsync(user);

                    _logger.LogInformation($"Password reset successfully for user: {user.Email}");

                    TempData["SuccessMessage"] = "Your password has been reset successfully. Please log in with your new password.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error resetting password for user: {model.Email}");
                    ModelState.AddModelError("", "An error occurred while resetting your password. Please try again.");
                    return View(model);
                }
            }
            return View(model);
        }

        private string GeneratePasswordResetToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var tokenBytes = new byte[32];
                rng.GetBytes(tokenBytes);
                return Convert.ToBase64String(tokenBytes)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");
            }
        }
    }
}