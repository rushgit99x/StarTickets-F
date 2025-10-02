using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Filters;
using StarTickets.Models;
using StarTickets.Models.ViewModels;
using StarTickets.Services.Interfaces;

namespace StarTickets.Controllers
{
    [RoleAuthorize("1")] // Admin only
    public class BookingManagementController : Controller
    {
        private readonly IBookingManagementService _service;
        private readonly ILogger<BookingManagementController> _logger;

        // Constructor with dependency injection
        public BookingManagementController(IBookingManagementService service, ILogger<BookingManagementController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET: BookingManagement
        // Displays a paginated list of bookings with optional filtering
        public async Task<IActionResult> Index(string search = "", PaymentStatus? paymentStatus = null, Models.BookingStatus? bookingStatus = null, int page = 1, int pageSize = 10)
        {
            try
            {
                // Verify user is logged in
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null) return RedirectToAction("Login", "Auth");// Redirect to login if not authenticated

                // Fetch paginated and filtered booking data
                var vm = await _service.GetIndexAsync(search, paymentStatus, bookingStatus, page, pageSize);
                return View(vm);// Render index view with view model
            }
            catch (Exception ex)
            {
                // Log error and return empty view on failure
                _logger.LogError(ex, "Error loading booking management index page. Search: {Search}, Page: {Page}", search, page);
                TempData["ErrorMessage"] = "An error occurred while loading bookings. Please try again.";
                return View(); // Return empty view model to prevent application crash
            }
        }

        // GET: BookingManagement/Details/5
        // Displays detailed information for a specific booking
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null) return RedirectToAction("Login", "Auth");

                // Fetch booking details
                var vm = await _service.GetDetailsAsync(id);
                if (vm == null) return NotFound(); // Return 404 if booking not found

                return View(vm); // Render details view with view model
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading booking details for BookingId: {BookingId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading booking details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: BookingManagement/Cancel/5
        // Cancels a specific booking with an optional reason
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null) return RedirectToAction("Login", "Auth");
                
                // Attempt to cancel booking
                var result = await _service.CancelAsync(id, reason);

                if (result.Success)
                {
                    // On success, show success message and redirect to details
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction("Details", new { id });
                }

                // On failure, show error message and redirect to details
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling booking. BookingId: {BookingId}, Reason: {Reason}", id, reason);
                TempData["ErrorMessage"] = "An unexpected error occurred while canceling the booking. Please try again.";
                return RedirectToAction("Details", new { id });
            }
        }

        // POST: BookingManagement/Delete/5
        // Deletes a specific booking
        [HttpPost]
        [ValidateAntiForgeryToken] // Prevent cross-site request forgery
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null) return RedirectToAction("Login", "Auth");

                // Attempt to delete booking
                var result = await _service.DeleteAsync(id);

                if (result.Success)
                {
                    // On success, show success message and redirect to index
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }

                // On failure, show error message and redirect to details
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                // Log error and redirect to index on failure
                _logger.LogError(ex, "Error deleting booking. BookingId: {BookingId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the booking. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}