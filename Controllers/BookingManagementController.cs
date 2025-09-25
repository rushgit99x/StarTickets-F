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

        public BookingManagementController(IBookingManagementService service, ILogger<BookingManagementController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET: BookingManagement
        public async Task<IActionResult> Index(string search = "", PaymentStatus? paymentStatus = null, Models.BookingStatus? bookingStatus = null, int page = 1, int pageSize = 10)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var vm = await _service.GetIndexAsync(search, paymentStatus, bookingStatus, page, pageSize);
            return View(vm);
        }

        // GET: BookingManagement/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var vm = await _service.GetDetailsAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        // POST: BookingManagement/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var result = await _service.CancelAsync(id, reason);
            if (result.Success) { TempData["SuccessMessage"] = result.Message; return RedirectToAction("Details", new { id }); }
            TempData["ErrorMessage"] = result.Message; return RedirectToAction("Details", new { id });
        }

        // POST: BookingManagement/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var result = await _service.DeleteAsync(id);
            if (result.Success) { TempData["SuccessMessage"] = result.Message; return RedirectToAction(nameof(Index)); }
            TempData["ErrorMessage"] = result.Message; return RedirectToAction("Details", new { id });
        }
    }
}


