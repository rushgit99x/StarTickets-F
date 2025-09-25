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
            _service = service;
            _logger = logger;
        }

        // GET: VenueManagement
        public async Task<IActionResult> Index(string searchTerm = "", string cityFilter = "",
            bool? activeFilter = null, int page = 1, int pageSize = 10)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var vm = await _service.GetIndexAsync(searchTerm, cityFilter, activeFilter, page, pageSize);
            return View(vm);
        }

        // GET: VenueManagement/Create
        public IActionResult Create()
        {
            return View(new CreateVenueViewModel());
        }

        // POST: VenueManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateVenueViewModel model)
        {
            if (ModelState.IsValid)
            {
                var ok = await _service.CreateAsync(model);
                if (ok)
                {
                    TempData["SuccessMessage"] = "Venue created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "An error occurred while creating the venue. Please try again.");
            }
            return View(model);
        }

        // GET: VenueManagement/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var vm = await _service.GetEditAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        // POST: VenueManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditVenueViewModel model)
        {
            if (ModelState.IsValid)
            {
                var ok = await _service.EditAsync(model);
                if (ok)
                {
                    TempData["SuccessMessage"] = "Venue updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "An error occurred while updating the venue. Please try again.");
            }
            return View(model);
        }

        // GET: VenueManagement/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var vm = await _service.GetDetailsAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        // GET: VenueManagement/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var vm = await _service.GetDetailsAsync(id);
            if (vm == null) return NotFound();
            if (vm.RecentEvents?.Any() == true)
            {
                TempData["ErrorMessage"] = "Cannot delete venue with existing events. Please remove or reassign events first.";
                return RedirectToAction(nameof(Index));
            }
            return View(vm.Venue);
        }

        // POST: VenueManagement/DeleteConfirmed/5
        [HttpPost, ActionName("Delete")]
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
            return RedirectToAction(nameof(Index));
        }

    }
}