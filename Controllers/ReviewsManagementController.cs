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
    public class ReviewsManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewsManagementController> _logger;
        private readonly IReviewsManagementService _service;

        public ReviewsManagementController(
            ApplicationDbContext context,
            ILogger<ReviewsManagementController> logger,
            IReviewsManagementService service)
        {
            _context = context;
            _logger = logger;
            _service = service;
        }

        // GET: ReviewsManagement
        public async Task<IActionResult> Index(
            string search = "",
            int? rating = null,
            bool? approved = null,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to reviews management index");
                    return RedirectToAction("Login", "Auth");
                }

                // Validate pagination parameters
                if (page < 1)
                {
                    _logger.LogWarning("Invalid page number: {Page}", page);
                    page = 1;
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    _logger.LogWarning("Invalid page size: {PageSize}", pageSize);
                    pageSize = 10;
                }

                // Validate rating parameter
                if (rating.HasValue && (rating.Value < 1 || rating.Value > 5))
                {
                    _logger.LogWarning("Invalid rating filter: {Rating}", rating);
                    rating = null;
                }

                // Validate search length
                if (!string.IsNullOrWhiteSpace(search) && search.Length > 200)
                {
                    _logger.LogWarning("Search term too long: {SearchLength}", search.Length);
                    search = search.Substring(0, 200);
                }

                var vm = await _service.GetIndexAsync(search, rating, approved, page, pageSize);
                return View(vm);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while loading reviews. Search: {Search}, Rating: {Rating}, Approved: {Approved}, Page: {Page}",
                    search, rating, approved, page);
                TempData["ErrorMessage"] = "Database error occurred while loading reviews.";
                return View();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while loading reviews. Search: {Search}, Rating: {Rating}, Approved: {Approved}, Page: {Page}",
                    search, rating, approved, page);
                TempData["ErrorMessage"] = "An error occurred while processing the request.";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while loading reviews. Search: {Search}, Rating: {Rating}, Approved: {Approved}, Page: {Page}",
                    search, rating, approved, page);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading reviews.";
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to approve review. ReviewId: {ReviewId}", id);
                    return RedirectToAction("Login", "Auth");
                }

                // Validate review ID
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid review ID for approval: {ReviewId}", id);
                    TempData["ErrorMessage"] = "Invalid review ID.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _service.ApproveAsync(id);

                if (result.Success)
                {
                    _logger.LogInformation("Review approved successfully. ReviewId: {ReviewId}, UserId: {UserId}", id, userId);
                }
                else
                {
                    _logger.LogWarning("Failed to approve review. ReviewId: {ReviewId}, Message: {Message}", id, result.Message);
                }

                TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error while approving review. ReviewId: {ReviewId}", id);
                TempData["ErrorMessage"] = "The review was modified by another user. Please refresh and try again.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while approving review. ReviewId: {ReviewId}", id);
                TempData["ErrorMessage"] = "Database error occurred while approving the review.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while approving review. ReviewId: {ReviewId}", id);
                TempData["ErrorMessage"] = "Unable to approve this review. It may have been deleted or modified.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while approving review. ReviewId: {ReviewId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while approving the review.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to reject review. ReviewId: {ReviewId}", id);
                    return RedirectToAction("Login", "Auth");
                }

                // Validate review ID
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid review ID for rejection: {ReviewId}", id);
                    TempData["ErrorMessage"] = "Invalid review ID.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _service.RejectAsync(id);

                if (result.Success)
                {
                    _logger.LogInformation("Review rejected successfully. ReviewId: {ReviewId}, UserId: {UserId}", id, userId);
                }
                else
                {
                    _logger.LogWarning("Failed to reject review. ReviewId: {ReviewId}, Message: {Message}", id, result.Message);
                }

                TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error while rejecting review. ReviewId: {ReviewId}", id);
                TempData["ErrorMessage"] = "The review was modified by another user. Please refresh and try again.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while rejecting review. ReviewId: {ReviewId}", id);
                TempData["ErrorMessage"] = "Database error occurred while rejecting the review.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while rejecting review. ReviewId: {ReviewId}", id);
                TempData["ErrorMessage"] = "Unable to reject this review. It may have been deleted or modified.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while rejecting review. ReviewId: {ReviewId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while rejecting the review.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to delete review. ReviewId: {ReviewId}", id);
                    return RedirectToAction("Login", "Auth");
                }

                // Validate review ID
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid review ID for deletion: {ReviewId}", id);
                    TempData["ErrorMessage"] = "Invalid review ID.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _service.DeleteAsync(id);

                if (result.Success)
                {
                    _logger.LogInformation("Review deleted successfully. ReviewId: {ReviewId}, UserId: {UserId}", id, userId);
                }
                else
                {
                    _logger.LogWarning("Failed to delete review. ReviewId: {ReviewId}, Message: {Message}", id, result.Message);
                }

                TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error while deleting review. ReviewId: {ReviewId}", id);
                TempData["ErrorMessage"] = "The review was modified by another user. Please refresh and try again.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting review. ReviewId: {ReviewId}", id);
                TempData["ErrorMessage"] = "Database error occurred while deleting the review. The review may be referenced by other records.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while deleting review. ReviewId: {ReviewId}", id);
                TempData["ErrorMessage"] = "Unable to delete this review. It may have already been deleted.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting review. ReviewId: {ReviewId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the review.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}