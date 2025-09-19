using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Filters;
using StarTickets.Models;

namespace StarTickets.Controllers
{
	[RoleAuthorize("1")] // Admin only
	public class ReviewsManagementController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly ILogger<ReviewsManagementController> _logger;

		public ReviewsManagementController(ApplicationDbContext context, ILogger<ReviewsManagementController> logger)
		{
			_context = context;
			_logger = logger;
		}

		// GET: ReviewsManagement
		public async Task<IActionResult> Index(string search = "", int? rating = null, bool? approved = null, int page = 1, int pageSize = 10)
		{
			var userId = HttpContext.Session.GetInt32("UserId");
			if (userId == null) return RedirectToAction("Login", "Auth");

			var query = _context.EventRatings
				.Include(r => r.Event)
				.Include(r => r.Customer)
				.AsQueryable();

			if (!string.IsNullOrWhiteSpace(search))
			{
				query = query.Where(r =>
					(r.Event != null && r.Event.EventName.Contains(search)) ||
					(r.Customer != null && ((r.Customer.FirstName + " " + r.Customer.LastName).Contains(search) || r.Customer.Email.Contains(search))) ||
					(r.Review != null && r.Review.Contains(search))
				);
			}

			if (rating.HasValue)
			{
				query = query.Where(r => r.Rating == rating.Value);
			}

			if (approved.HasValue)
			{
				query = query.Where(r => r.IsApproved == approved.Value);
			}

			var total = await query.CountAsync();
			var items = await query
				.OrderByDescending(r => r.CreatedAt)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var vm = new Models.ViewModels.ReviewsManagementListViewModel
			{
				Ratings = items,
				Search = search,
				RatingFilter = rating,
				ApprovedFilter = approved,
				CurrentPage = page,
				PageSize = pageSize,
				TotalItems = total,
				TotalPages = (int)Math.Ceiling(total / (double)pageSize)
			};

			return View(vm);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Approve(int id)
		{
			var rating = await _context.EventRatings.FindAsync(id);
			if (rating == null)
			{
				TempData["ErrorMessage"] = "Review not found.";
				return RedirectToAction(nameof(Index));
			}

			rating.IsApproved = true;
			await _context.SaveChangesAsync();
			TempData["SuccessMessage"] = "Review approved.";
			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Reject(int id)
		{
			var rating = await _context.EventRatings.FindAsync(id);
			if (rating == null)
			{
				TempData["ErrorMessage"] = "Review not found.";
				return RedirectToAction(nameof(Index));
			}

			rating.IsApproved = false;
			await _context.SaveChangesAsync();
			TempData["SuccessMessage"] = "Review rejected.";
			return RedirectToAction(nameof(Index));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id)
		{
			try
			{
				var rating = await _context.EventRatings.FindAsync(id);
				if (rating == null)
				{
					TempData["ErrorMessage"] = "Review not found.";
					return RedirectToAction(nameof(Index));
				}

				_context.EventRatings.Remove(rating);
				await _context.SaveChangesAsync();
				TempData["SuccessMessage"] = "Review deleted.";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to delete review {RatingId}", id);
				TempData["ErrorMessage"] = "Failed to delete review.";
			}

			return RedirectToAction(nameof(Index));
		}
	}
}
