using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Filters;
using StarTickets.Models;
using StarTickets.Services.Interfaces;

namespace StarTickets.Controllers
{
	[RoleAuthorize("1")] // Admin only
	public class ReviewsManagementController : Controller
	{
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewsManagementController> _logger;
        private readonly IReviewsManagementService _service;

        public ReviewsManagementController(ApplicationDbContext context, ILogger<ReviewsManagementController> logger, IReviewsManagementService service)
        {
            _context = context;
            _logger = logger;
            _service = service;
        }

		// GET: ReviewsManagement
		public async Task<IActionResult> Index(string search = "", int? rating = null, bool? approved = null, int page = 1, int pageSize = 10)
		{
			var userId = HttpContext.Session.GetInt32("UserId");
			if (userId == null) return RedirectToAction("Login", "Auth");

            var vm = await _service.GetIndexAsync(search, rating, approved, page, pageSize);
            return View(vm);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Approve(int id)
		{
            var result = await _service.ApproveAsync(id);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Reject(int id)
		{
            var result = await _service.RejectAsync(id);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id)
		{
            var result = await _service.DeleteAsync(id);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
		}
	}
}
