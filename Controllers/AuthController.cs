using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StarTickets.Models.ViewModels;
using StarTickets.Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace StarTickets.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // GET: /Account/Register
        public IActionResult Register() => View();

        // POST: /Auth/Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var success = await _authService.RegisterAsync(model);
                    if (!success)
                    {
                        ModelState.AddModelError("", "Email already exists or an error occurred during registration.");
                        return View(model);
                    }

                    TempData["SuccessMessage"] = "Registration successful! Please check your email for a welcome message.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during user registration for email: {Email}", model.Email);
                    ModelState.AddModelError("", "An unexpected error occurred during registration. Please try again later.");
                    return View(model);
                }
            }
            return View(model);
        }

        // GET: /Account/Login
        public IActionResult Login() => View();

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _authService.LoginAsync(model);
                    if (user != null)
                    {
                        HttpContext.Session.SetInt32("UserId", user.UserId);
                        HttpContext.Session.SetString("Role", user.Role.ToString());

                        return user.Role switch
                        {
                            1 => RedirectToAction("Index", "Admin"),
                            2 => RedirectToAction("Index", "EventOrganizer"),
                            3 => RedirectToAction("Index", "Home"),
                            _ => RedirectToAction("Login")
                        };
                    }
                    ModelState.AddModelError("", "Invalid login attempt");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during login attempt for email: {Email}", model.Email);
                    ModelState.AddModelError("", "An unexpected error occurred during login. Please try again later.");
                    return View(model);
                }
            }
            return View(model);
        }

        public IActionResult Logout()
        {
            try
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during logout");
                return RedirectToAction("Login");
            }
        }

        // GET: /Account/ForgotPassword
        public IActionResult ForgotPassword() => View();

        // POST: /Account/ForgotPassword
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var success = await _authService.ForgotPasswordAsync(model, Request.Scheme);
                    // If service failed (e.g., email not found or some error)
                    if (!success)
                    {
                        ModelState.AddModelError("Email", "Email not found or an error occurred.");
                        return View(model);
                    }

                    TempData["SuccessMessage"] = "If the email address exists in our system, you will receive a password reset link shortly.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during forgot password request for email: {Email}", model.Email);
                    ModelState.AddModelError("", "An unexpected error occurred. Please try again later.");
                    return View(model);
                }
            }
            return View(model);
        }

        // GET: /Account/ResetPassword
        // Displays the password reset page if the token and email are valid.
        // If the token/email is missing, invalid, or expired → redirects to Login with an error message.
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Invalid password reset link.";
                return RedirectToAction("Login");
            }

            try
            {
                var user = await _authService.ValidateResetTokenAsync(email, token);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during password reset token validation for email: {Email}", email);
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                return RedirectToAction("Login");
            }
        }

        // POST: /Auth/ResetPassword
        // Handles password reset form submission.
        // If model is valid and reset succeeds → redirects to Login with success message.
        // If reset fails → redirects to Login with error message.
        // If an exception occurs → logs error and redirects with a generic error message.
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var success = await _authService.ResetPasswordAsync(model);
                    if (!success)
                    {
                        TempData["ErrorMessage"] = "Invalid or expired password reset link.";
                        return RedirectToAction("Login");
                    }

                    TempData["SuccessMessage"] = "Your password has been reset successfully. Please log in with your new password.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during password reset for email: {Email}", model.Email);
                    TempData["ErrorMessage"] = "An unexpected error occurred while resetting your password. Please try again later.";
                    return RedirectToAction("Login");
                }
            }
            return View(model);
        }
    }
}