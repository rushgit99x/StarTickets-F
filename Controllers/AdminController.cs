using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Filters;
using StarTickets.Models;
using StarTickets.Models.ViewModels;
using StarTickets.Services.Interfaces;

namespace StarTickets.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [RoleAuthorize("1")]
        public IActionResult Index()
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");

                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to admin dashboard - no user session found");
                    return RedirectToAction("Login", "Auth");
                }

                _logger.LogInformation("Admin dashboard accessed by user {UserId}", userId.Value);

                var model = _adminService.BuildDashboard(userId.Value);

                if (model == null)
                {
                    _logger.LogError("Dashboard model returned null for user {UserId}", userId.Value);
                    TempData["ErrorMessage"] = "Unable to load dashboard. Please try again.";
                    return View("Error");
                }

                return View(model);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt to admin dashboard");
                TempData["ErrorMessage"] = "You don't have permission to access this resource.";
                return RedirectToAction("Login", "Auth");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while building admin dashboard");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";
                return View("Error");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while accessing admin dashboard");
                TempData["ErrorMessage"] = "A database error occurred. Please contact support.";
                return View("Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred in admin dashboard for user {UserId}",
                    HttpContext.Session.GetInt32("UserId"));
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                return View("Error");
            }
        }
    }
}