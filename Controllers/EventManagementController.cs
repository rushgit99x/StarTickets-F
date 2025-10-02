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
        // Displays a paginated list of events with optional filtering
        public async Task<IActionResult> Index(string searchTerm = "", int categoryFilter = 0,
            EventStatus? statusFilter = null, int page = 1, int pageSize = 10)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to EventManagement/Index");
                    return RedirectToAction("Login", "Auth");
                }

                var viewModel = await _service.GetIndexAsync(searchTerm, categoryFilter, statusFilter, page, pageSize);
                return View(viewModel);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while loading events index");
                TempData["ErrorMessage"] = "A database error occurred while loading events. Please try again.";
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while loading events index");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please contact support if the problem persists.";
                return View();
            }
        }

        // GET: EventManagement/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                var viewModel = await _service.GetCreateFormAsync();
                return View(viewModel);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while loading create event form");
                TempData["ErrorMessage"] = "A database error occurred while loading the form. Please try again.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while loading create event form");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the form. Please contact support.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: EventManagement/Create
        // Handles submission of the create event form
        [HttpPost]
        [ValidateAntiForgeryToken] // Prevent cross-site request forgery
        public async Task<IActionResult> Create(CreateEventViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var success = await _service.CreateAsync(model);
                    if (success)
                    {
                        _logger.LogInformation("Event created successfully: {EventTitle}", model.Title);
                        TempData["SuccessMessage"] = "Event created successfully!";
                        return RedirectToAction(nameof(Index));
                    }

                    _logger.LogWarning("Event creation failed for: {EventTitle}", model.Title);
                    ModelState.AddModelError("", "An error occurred while creating the event. Please try again.");
                }
                // Reload form data on failure
                var reload = await _service.GetCreateFormAsync();
                model.Categories = reload.Categories;
                model.Venues = reload.Venues;
                model.Organizers = reload.Organizers;
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while creating event: {EventTitle}", model.Title);
                TempData["ErrorMessage"] = "A database error occurred. The event could not be saved. Please try again.";

                try
                {
                    var reload = await _service.GetCreateFormAsync();
                    model.Categories = reload.Categories;
                    model.Venues = reload.Venues;
                    model.Organizers = reload.Organizers;
                    return View(model);
                }
                catch
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while creating event: {EventTitle}", model.Title);
                ModelState.AddModelError("", "Invalid operation. Please check your input and try again.");

                try
                {
                    var reload = await _service.GetCreateFormAsync();
                    model.Categories = reload.Categories;
                    model.Venues = reload.Venues;
                    model.Organizers = reload.Organizers;
                    return View(model);
                }
                catch
                {
                    return RedirectToAction(nameof(Index));// Redirect to index if reload fails
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating event: {EventTitle}", model.Title);
                TempData["ErrorMessage"] = "An unexpected error occurred. Please contact support if the problem persists.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: EventManagement/EditForm/5
        // Displays the form for editing an existing event
        public async Task<IActionResult> EditForm(int id)
        {
            try
            {
                var vm = await _service.GetEditFormAsync(id);
                if (vm == null)
                {
                    _logger.LogWarning("Event not found for edit: EventId={EventId}", id);
                    TempData["ErrorMessage"] = "Event not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(vm);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while loading edit form for EventId={EventId}", id);
                TempData["ErrorMessage"] = "A database error occurred while loading the event. Please try again.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while loading edit form for EventId={EventId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred. Please contact support.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: EventManagement/Edit/5
        // Handles submission of the edit event form
        [HttpPost]
        [ValidateAntiForgeryToken]// Prevent cross-site request forgery
        public async Task<IActionResult> Edit(EditEventViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var success = await _service.EditAsync(model);
                    if (success)
                    {
                        _logger.LogInformation("Event updated successfully: EventId={EventId}", model.EventId);
                        TempData["SuccessMessage"] = "Event updated successfully!";
                        return RedirectToAction(nameof(Index));
                    }

                    _logger.LogWarning("Event update failed for EventId={EventId}", model.EventId);
                    ModelState.AddModelError("", "An error occurred while updating the event. Please try again.");
                }
                // Attempt to reload form data
                var reload = await _service.GetEditFormAsync(model.EventId);
                if (reload != null)
                {
                    model.Categories = reload.Categories;
                    model.Venues = reload.Venues;
                    model.Organizers = reload.Organizers;
                }
                return View("EditForm", model);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error while updating EventId={EventId}", model.EventId);
                TempData["ErrorMessage"] = "The event was modified by another user. Please reload and try again.";
                return RedirectToAction(nameof(EditForm), new { id = model.EventId });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating EventId={EventId}", model.EventId);
                TempData["ErrorMessage"] = "A database error occurred. The event could not be updated. Please try again.";

                try
                {
                    var reload = await _service.GetEditFormAsync(model.EventId);
                    if (reload != null)
                    {
                        model.Categories = reload.Categories;
                        model.Venues = reload.Venues;
                        model.Organizers = reload.Organizers;
                    }
                    return View("EditForm", model);
                }
                catch
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while updating EventId={EventId}", model.EventId);
                ModelState.AddModelError("", "Invalid operation. Please check your input and try again.");

                try
                {
                    var reload = await _service.GetEditFormAsync(model.EventId);
                    if (reload != null)
                    {
                        model.Categories = reload.Categories;
                        model.Venues = reload.Venues;
                        model.Organizers = reload.Organizers;
                    }
                    return View("EditForm", model);
                }
                catch
                {
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating EventId={EventId}", model.EventId);
                TempData["ErrorMessage"] = "An unexpected error occurred. Please contact support if the problem persists.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: EventManagement/Details/5
        // Displays detailed information for a specific event
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var vm = await _service.GetDetailsAsync(id);
                if (vm == null)
                {
                    _logger.LogWarning("Event not found for details: EventId={EventId}", id);
                    TempData["ErrorMessage"] = "Event not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(vm);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while loading event details for EventId={EventId}", id);
                TempData["ErrorMessage"] = "A database error occurred while loading event details. Please try again.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while loading event details for EventId={EventId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred. Please contact support.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: EventManagement/UpdateStatus/5
        // Updates the status of an event
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, EventStatus status)
        {
            try
            {
                var result = await _service.UpdateStatusAsync(id, status);

                if (result.Success)
                {
                    _logger.LogInformation("Event status updated: EventId={EventId}, NewStatus={Status}", id, status);
                }
                else
                {
                    _logger.LogWarning("Event status update failed: EventId={EventId}, Status={Status}, Message={Message}",
                        id, status, result.Message);
                }

                return Json(new { success = result.Success, message = result.Message });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error while updating status for EventId={EventId}", id);
                return Json(new
                {
                    success = false,
                    message = "The event was modified by another user. Please refresh the page and try again."
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating status for EventId={EventId}", id);
                return Json(new
                {
                    success = false,
                    message = "A database error occurred. Please try again."
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while updating status for EventId={EventId}", id);
                return Json(new
                {
                    success = false,
                    message = "Invalid status update operation. Please check your request."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating status for EventId={EventId}", id);
                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred. Please contact support."
                });
            }
        }

        // GET: EventManagement/Delete/5 - Shows confirmation page
        // Displays the confirmation page for deleting an event
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var vm = await _service.GetDetailsAsync(id);
                if (vm?.Event == null)
                {
                    _logger.LogWarning("Event not found for delete confirmation: EventId={EventId}", id);
                    TempData["ErrorMessage"] = "Event not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(vm.Event);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while loading delete confirmation for EventId={EventId}", id);
                TempData["ErrorMessage"] = "A database error occurred. Please try again.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while loading delete confirmation for EventId={EventId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred. Please contact support.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: EventManagement/Delete/5 - Actually deletes the event
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _service.DeleteAsync(id);

                if (result.Success)
                {
                    _logger.LogInformation("Event deleted successfully: EventId={EventId}", id);
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogWarning("Event deletion failed: EventId={EventId}, Message={Message}", id, result.Message);
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction(nameof(Delete), new { id = id });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error while deleting EventId={EventId}", id);
                TempData["ErrorMessage"] = "The event was modified by another user. Please reload and try again.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while deleting EventId={EventId}", id);
                TempData["ErrorMessage"] = "A database error occurred. The event could not be deleted. It may have associated bookings.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while deleting EventId={EventId}", id);
                TempData["ErrorMessage"] = "Cannot delete this event. It may have active bookings or dependencies.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting EventId={EventId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred. Please contact support if the problem persists.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}