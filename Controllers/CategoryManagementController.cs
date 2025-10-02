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
    public class CategoryManagementController : Controller
    {
        private readonly ICategoryManagementService _service;
        private readonly ILogger<CategoryManagementController> _logger;

        public CategoryManagementController(ICategoryManagementService service, ILogger<CategoryManagementController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET: CategoryManagement
        public async Task<IActionResult> Index(string searchTerm = "", int page = 1, int pageSize = 10)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null) return RedirectToAction("Login", "Auth");

                var viewModel = await _service.GetIndexAsync(searchTerm, page, pageSize);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading category index page. SearchTerm: {SearchTerm}, Page: {Page}", searchTerm, page);
                TempData["ErrorMessage"] = "An error occurred while loading categories. Please try again.";
                return View();
            }
        }

        // GET: CategoryManagement/Create
        public IActionResult Create()
        {
            try
            {
                return View(new CreateCategoryViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading create category page");
                TempData["ErrorMessage"] = "An error occurred while loading the page. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CategoryManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCategoryViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var ok = await _service.CreateAsync(model);
                    if (ok)
                    {
                        TempData["SuccessMessage"] = "Category created successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    ModelState.AddModelError("CategoryName", "A category with this name already exists.");
                }
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while creating category: {CategoryName}", model?.CategoryName);
                ModelState.AddModelError("", "A database error occurred. The category may already exist or there may be a constraint violation.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating category: {CategoryName}", model?.CategoryName);
                ModelState.AddModelError("", "An unexpected error occurred while creating the category. Please try again.");
                return View(model);
            }
        }

        // GET: CategoryManagement/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var vm = await _service.GetEditAsync(id);
                if (vm == null)
                {
                    _logger.LogWarning("Category not found for edit. CategoryId: {CategoryId}", id);
                    return NotFound();
                }
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading edit page for category: {CategoryId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the category. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CategoryManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditCategoryViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var ok = await _service.EditAsync(model);
                    if (ok)
                    {
                        TempData["SuccessMessage"] = "Category updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    ModelState.AddModelError("CategoryName", "A category with this name already exists.");
                }
                return View(model);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error occurred while updating category: {CategoryId}", model?.CategoryId);
                ModelState.AddModelError("", "The category was modified by another user. Please refresh and try again.");
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while updating category: {CategoryId}", model?.CategoryId);
                ModelState.AddModelError("", "A database error occurred while updating the category.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating category: {CategoryId}", model?.CategoryId);
                ModelState.AddModelError("", "An unexpected error occurred while updating the category. Please try again.");
                return View(model);
            }
        }

        // GET: CategoryManagement/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var vm = await _service.GetDetailsAsync(id);
                if (vm == null)
                {
                    _logger.LogWarning("Category not found for details. CategoryId: {CategoryId}", id);
                    return NotFound();
                }
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading details for category: {CategoryId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading category details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Route("CategoryManagement/Delete/{id:int}")]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            try
            {
                var result = await _service.DeleteAjaxAsync(id);
                return Json(new { success = result.Success, message = result.Message });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while deleting category via AJAX: {CategoryId}", id);
                return Json(new { success = false, message = "Cannot delete this category because it is being used by other records." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting category via AJAX: {CategoryId}", id);
                return Json(new { success = false, message = "An unexpected error occurred while deleting the category." });
            }
        }

        // GET: CategoryManagement/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await _service.GetCategoryForDeleteAsync(id);
                if (category == null)
                {
                    _logger.LogWarning("Category not found for delete. CategoryId: {CategoryId}", id);
                    return NotFound();
                }
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading delete page for category: {CategoryId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the category. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: CategoryManagement/DeleteConfirmed/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _service.DeleteAsync(id);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Delete), new { id = id });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while deleting category: {CategoryId}", id);
                TempData["ErrorMessage"] = "Cannot delete this category because it is being used by other records.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting category: {CategoryId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the category. Please try again.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }
        }
    }
}