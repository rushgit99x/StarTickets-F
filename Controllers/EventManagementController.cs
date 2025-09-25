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
    public class EventManagementController : Controller
    {
        private readonly IEventManagementService _service;
        private readonly ILogger<EventManagementController> _logger;

        public EventManagementController(IEventManagementService service, ILogger<EventManagementController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET: EventManagement
        public async Task<IActionResult> Index(string searchTerm = "", int categoryFilter = 0,
            EventStatus? statusFilter = null, int page = 1, int pageSize = 10)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var viewModel = await _service.GetIndexAsync(searchTerm, categoryFilter, statusFilter, page, pageSize);
            return View(viewModel);
        }

        // GET: EventManagement/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = await _service.GetCreateFormAsync();
            return View(viewModel);
        }

        // POST: EventManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEventViewModel model)
        {
            if (ModelState.IsValid)
            {
                var success = await _service.CreateAsync(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Event created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "An error occurred while creating the event. Please try again.");
            }

            var reload = await _service.GetCreateFormAsync();
            model.Categories = reload.Categories;
            model.Venues = reload.Venues;
            model.Organizers = reload.Organizers;
            return View(model);
        }

        // GET: EventManagement/EditForm/5
        public async Task<IActionResult> EditForm(int id)
        {
            var vm = await _service.GetEditFormAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        // POST: EventManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditEventViewModel model)
        {
            if (ModelState.IsValid)
            {
                var success = await _service.EditAsync(model);
                if (success)
                {
                    TempData["SuccessMessage"] = "Event updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError("", "An error occurred while updating the event. Please try again.");
            }

            var reload = await _service.GetEditFormAsync(model.EventId);
            if (reload != null)
            {
                model.Categories = reload.Categories;
                model.Venues = reload.Venues;
                model.Organizers = reload.Organizers;
            }
            return View("EditForm", model);
        }

        // GET: EventManagement/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var vm = await _service.GetDetailsAsync(id);
            if (vm == null) return NotFound();
            return View(vm);
        }

        // POST: EventManagement/UpdateStatus/5
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, EventStatus status)
        {
            var result = await _service.UpdateStatusAsync(id, status);
            return Json(new { success = result.Success, message = result.Message });
        }

        // GET: EventManagement/Delete/5 - Shows confirmation page
        public async Task<IActionResult> Delete(int id)
        {
            var vm = await _service.GetDetailsAsync(id);
            if (vm?.Event == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(vm.Event);
        }

        // POST: EventManagement/Delete/5 - Actually deletes the event
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