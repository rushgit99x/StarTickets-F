using Microsoft.AspNetCore.Mvc;
using StarTickets.Models.ViewModels;
using StarTickets.Services.Interfaces;
using System.Threading.Tasks;

namespace StarTickets.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // GET: /Account/Register
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
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
            return View(model);
        }

        // GET: /Account/Login
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
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
                var success = await _authService.ForgotPasswordAsync(model, Request.Scheme);
                if (!success)
                {
                    ModelState.AddModelError("Email", "Email not found or an error occurred.");
                    return View(model);
                }

                TempData["SuccessMessage"] = "If the email address exists in our system, you will receive a password reset link shortly.";
                return RedirectToAction("Login");
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

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
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
            return View(model);
        }
    }
}