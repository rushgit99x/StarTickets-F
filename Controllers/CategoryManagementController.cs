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
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var viewModel = await _service.GetIndexAsync(searchTerm, page, pageSize);
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

        // GET: CategoryManagement/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var vm = await _service.GetEditAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        // POST: CategoryManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditCategoryViewModel model)
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

        // GET: CategoryManagement/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var vm = await _service.GetDetailsAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        [HttpPost]
        [Route("CategoryManagement/Delete/{id:int}")]
        public async Task<IActionResult> DeleteAjax(int id)
        {
            var result = await _service.DeleteAjaxAsync(id);
            return Json(new { success = result.Success, message = result.Message });
        }
        // GET: CategoryManagement/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _service.GetCategoryForDeleteAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        // POST: CategoryManagement/DeleteConfirmed/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
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
    }
}