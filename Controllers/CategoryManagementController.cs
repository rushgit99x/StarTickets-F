using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Filters;
using StarTickets.Models;
using StarTickets.Models.ViewModels;

namespace StarTickets.Controllers
{
    [RoleAuthorize("1")] // Admin only
    public class CategoryManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoryManagementController> _logger;

        public CategoryManagementController(ApplicationDbContext context, ILogger<CategoryManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: CategoryManagement
        public async Task<IActionResult> Index(string searchTerm = "", int page = 1, int pageSize = 10)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var query = _context.EventCategories
                .Include(c => c.Events)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c => c.CategoryName.Contains(searchTerm) ||
                                        c.Description!.Contains(searchTerm));
            }

            var totalCategories = await query.CountAsync();
            var categories = await query
                .OrderBy(c => c.CategoryName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new CategoryManagementViewModel
            {
                Categories = categories,
                SearchTerm = searchTerm,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCategories = totalCategories,
                TotalPages = (int)Math.Ceiling((double)totalCategories / pageSize)
            };

            return View(viewModel);
        }

        // GET: CategoryManagement/Create
        public IActionResult Create()
        {
            return View(new CreateCategoryViewModel());
        }

        // POST: CategoryManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Check if category name already exists
                    var existingCategory = await _context.EventCategories
                        .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == model.CategoryName.ToLower());

                    if (existingCategory != null)
                    {
                        ModelState.AddModelError("CategoryName", "A category with this name already exists.");
                        return View(model);
                    }

                    var category = new EventCategory
                    {
                        CategoryName = model.CategoryName,
                        Description = model.Description,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.EventCategories.Add(category);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Category created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating category");
                    ModelState.AddModelError("", "An error occurred while creating the category. Please try again.");
                }
            }

            return View(model);
        }

        // GET: CategoryManagement/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.EventCategories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var viewModel = new EditCategoryViewModel
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description
            };

            return View(viewModel);
        }

        // POST: CategoryManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditCategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var category = await _context.EventCategories.FindAsync(model.CategoryId);
                    if (category == null)
                    {
                        return NotFound();
                    }

                    // Check if category name already exists (excluding current category)
                    var existingCategory = await _context.EventCategories
                        .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == model.CategoryName.ToLower() &&
                                                 c.CategoryId != model.CategoryId);

                    if (existingCategory != null)
                    {
                        ModelState.AddModelError("CategoryName", "A category with this name already exists.");
                        return View(model);
                    }

                    category.CategoryName = model.CategoryName;
                    category.Description = model.Description;

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Category updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating category");
                    ModelState.AddModelError("", "An error occurred while updating the category. Please try again.");
                }
            }

            return View(model);
        }

        // GET: CategoryManagement/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var category = await _context.EventCategories
                .Include(c => c.Events!.Where(e => e.IsActive))
                .ThenInclude(e => e.Venue)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            var totalEvents = category.Events?.Count ?? 0;
            var activeEvents = category.Events?.Count(e => e.Status == EventStatus.Published) ?? 0;
            var upcomingEvents = category.Events?.Count(e => e.EventDate > DateTime.UtcNow) ?? 0;

            var viewModel = new CategoryDetailsViewModel
            {
                Category = category,
                TotalEvents = totalEvents,
                ActiveEvents = activeEvents,
                UpcomingEvents = upcomingEvents,
                RecentEvents = category.Events?.OrderByDescending(e => e.CreatedAt).Take(10).ToList() ?? new List<Event>()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route("CategoryManagement/Delete/{id:int}")]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            try
            {
                var category = await _context.EventCategories
                    .Include(c => c.Events)
                    .FirstOrDefaultAsync(c => c.CategoryId == id);

                if (category == null)
                {
                    return Json(new { success = false, message = "Category not found." });
                }

                // Check if category has events
                if (category.Events?.Any() == true)
                {
                    return Json(new { success = false, message = "Cannot delete category with existing events. Please move or delete the events first." });
                }

                _context.EventCategories.Remove(category);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Category deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category");
                return Json(new { success = false, message = "An error occurred while deleting the category." });
            }
        }
        // GET: CategoryManagement/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.EventCategories
                .Include(c => c.Events)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: CategoryManagement/DeleteConfirmed/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var category = await _context.EventCategories
                    .Include(c => c.Events)
                    .FirstOrDefaultAsync(c => c.CategoryId == id);

                if (category == null)
                {
                    TempData["ErrorMessage"] = "Category not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if category has events
                if (category.Events?.Any() == true)
                {
                    TempData["ErrorMessage"] = "Cannot delete category with existing events. Please move or delete the events first.";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }

                _context.EventCategories.Remove(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Category deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category");
                TempData["ErrorMessage"] = "An error occurred while deleting the category. Please try again.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }
        }
    }
}