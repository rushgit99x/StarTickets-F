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
    public class VenueManagementController : Controller
    {
        private readonly IVenueManagementService _service;
        private readonly ILogger<VenueManagementController> _logger;

        public VenueManagementController(IVenueManagementService service, ILogger<VenueManagementController> logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: VenueManagement
        public async Task<IActionResult> Index(string searchTerm = "", string cityFilter = "",
            bool? activeFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to VenueManagement Index");
                    return RedirectToAction("Login", "Auth");
                }

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

                var vm = await _service.GetIndexAsync(searchTerm, cityFilter, activeFilter, page, pageSize);
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving venue list. SearchTerm: {SearchTerm}, CityFilter: {CityFilter}, Page: {Page}",
                    searchTerm, cityFilter, page);
                TempData["ErrorMessage"] = "An error occurred while loading venues. Please try again.";
                return View();
            }
        }

        // GET: VenueManagement/Create
        public IActionResult Create()
        {
            try
            {
                return View(new CreateVenueViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading venue creation form");
                TempData["ErrorMessage"] = "An error occurred while loading the form. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: VenueManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateVenueViewModel model)
        {
            try
            {
                if (model == null)
                {
                    _logger.LogWarning("Create venue called with null model");
                    throw new ArgumentNullException(nameof(model));
                }

                if (ModelState.IsValid)
                {
                    var ok = await _service.CreateAsync(model);
                    if (ok)
                    {
                        _logger.LogInformation("Venue created successfully: {VenueName}", model.Name);
                        TempData["SuccessMessage"] = "Venue created successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    ModelState.AddModelError("", "An error occurred while creating the venue. Please try again.");
                }
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while creating venue: {VenueName}", model?.Name);
                ModelState.AddModelError("", "A database error occurred. The venue name or location may already exist.");
                return View(model);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument while creating venue: {VenueName}", model?.Name);
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating venue: {VenueName}", model?.Name);
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: VenueManagement/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid venue ID provided for edit: {Id}", id);
                    return BadRequest("Invalid venue ID");
                }

                var vm = await _service.GetEditAsync(id);
                if (vm == null)
                {
                    _logger.LogWarning("Venue not found for edit: {Id}", id);
                    return NotFound();
                }
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading venue for edit: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the venue. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: VenueManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditVenueViewModel model)
        {
            try
            {
                if (model == null)
                {
                    _logger.LogWarning("Edit venue called with null model");
                    throw new ArgumentNullException(nameof(model));
                }

                if (model.VenueId <= 0)
                {
                    _logger.LogWarning("Invalid venue ID in edit model: {Id}", model.VenueId);
                    return BadRequest("Invalid venue ID");
                }

                if (ModelState.IsValid)
                {
                    var ok = await _service.EditAsync(model);
                    if (ok)
                    {
                        _logger.LogInformation("Venue updated successfully: {VenueId}", model.VenueId);
                        TempData["SuccessMessage"] = "Venue updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    ModelState.AddModelError("", "An error occurred while updating the venue. Please try again.");
                }
                return View(model);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error while updating venue: {VenueId}", model?.VenueId);
                ModelState.AddModelError("", "The venue was modified by another user. Please reload and try again.");
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating venue: {VenueId}", model?.VenueId);
                ModelState.AddModelError("", "A database error occurred. Please check your input and try again.");
                return View(model);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument while updating venue: {VenueId}", model?.VenueId);
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating venue: {VenueId}", model?.VenueId);
                TempData["ErrorMessage"] = "An unexpected error occurred. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: VenueManagement/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid venue ID provided for details: {Id}", id);
                    return BadRequest("Invalid venue ID");
                }

                var vm = await _service.GetDetailsAsync(id);
                if (vm == null)
                {
                    _logger.LogWarning("Venue not found for details: {Id}", id);
                    return NotFound();
                }
                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading venue details: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while loading venue details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: VenueManagement/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid venue ID provided for delete: {Id}", id);
                    return BadRequest("Invalid venue ID");
                }

                var vm = await _service.GetDetailsAsync(id);
                if (vm == null)
                {
                    _logger.LogWarning("Venue not found for delete: {Id}", id);
                    return NotFound();
                }

                if (vm.RecentEvents?.Any() == true)
                {
                    _logger.LogWarning("Attempt to delete venue with existing events: {Id}", id);
                    TempData["ErrorMessage"] = "Cannot delete venue with existing events. Please remove or reassign events first.";
                    return RedirectToAction(nameof(Index));
                }

                return View(vm.Venue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading venue for deletion: {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the venue. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: VenueManagement/DeleteConfirmed/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid venue ID provided for delete confirmation: {Id}", id);
                    TempData["ErrorMessage"] = "Invalid venue ID";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _service.DeleteAsync(id);
                if (result.Success)
                {
                    _logger.LogInformation("Venue deleted successfully: {Id}", id);
                    TempData["SuccessMessage"] = result.Message;
                }
                else
                {
                    _logger.LogWarning("Failed to delete venue: {Id}. Reason: {Message}", id, result.Message);
                    TempData["ErrorMessage"] = result.Message;
                }
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting venue: {Id}", id);
                TempData["ErrorMessage"] = "Cannot delete venue due to existing references. Please remove associated data first.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while deleting venue: {Id}", id);
                TempData["ErrorMessage"] = "Cannot delete this venue. " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting venue: {Id}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred while deleting the venue. Please try again later.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}