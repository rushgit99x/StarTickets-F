using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Filters;
using StarTickets.Models;
using StarTickets.Services.Interfaces;

namespace StarTickets.Controllers
{
    [RoleAuthorize("1")] // Admin only
    public class DashboardStatsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IDashboardStatsService _service;
        private readonly ILogger<DashboardStatsController> _logger;

        public DashboardStatsController(
            ApplicationDbContext context,
            IDashboardStatsService service,
            ILogger<DashboardStatsController> logger)
        {
            _context = context;
            _service = service;
            _logger = logger;
        }

        // GET: API endpoint for dashboard statistics
        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var data = await _service.GetStatsAsync();
                return Json(data);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while fetching dashboard statistics");
                return Json(new { success = false, message = "Database error occurred while retrieving statistics." });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while fetching dashboard statistics");
                return Json(new { success = false, message = "Invalid operation occurred while retrieving statistics." });
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Timeout occurred while fetching dashboard statistics");
                return Json(new { success = false, message = "Request timed out. Please try again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching dashboard statistics");
                return Json(new { success = false, message = "An unexpected error occurred while retrieving statistics." });
            }
        }

        // GET: API endpoint for event status distribution
        [HttpGet]
        public async Task<IActionResult> GetEventStatusDistribution()
        {
            try
            {
                var data = await _service.GetEventStatusDistributionAsync();

                if (data == null)
                {
                    _logger.LogWarning("Event status distribution returned null data");
                    return Json(new { success = false, message = "No event status data available." });
                }

                return Json(new { success = true, data });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while fetching event status distribution");
                return Json(new { success = false, message = "Database error occurred while retrieving event status distribution." });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while fetching event status distribution");
                return Json(new { success = false, message = "Invalid operation occurred while retrieving event status distribution." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching event status distribution");
                return Json(new { success = false, message = "An unexpected error occurred while retrieving event status distribution." });
            }
        }

        // GET: API endpoint for category-wise event distribution
        [HttpGet]
        public async Task<IActionResult> GetCategoryDistribution()
        {
            try
            {
                var data = await _service.GetCategoryDistributionAsync();

                if (data == null)
                {
                    _logger.LogWarning("Category distribution returned null data");
                    return Json(new { success = false, message = "No category distribution data available." });
                }

                return Json(new { success = true, data });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while fetching category distribution");
                return Json(new { success = false, message = "Database error occurred while retrieving category distribution." });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while fetching category distribution");
                return Json(new { success = false, message = "Invalid operation occurred while retrieving category distribution." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching category distribution");
                return Json(new { success = false, message = "An unexpected error occurred while retrieving category distribution." });
            }
        }

        // GET: API endpoint for top performing events
        [HttpGet]
        public async Task<IActionResult> GetTopPerformingEvents(int count = 5)
        {
            try
            {
                // Validate input
                if (count <= 0)
                {
                    _logger.LogWarning("Invalid count parameter provided: {Count}", count);
                    return Json(new { success = false, message = "Count must be greater than zero." });
                }

                if (count > 100)
                {
                    _logger.LogWarning("Count parameter exceeds maximum: {Count}", count);
                    return Json(new { success = false, message = "Count cannot exceed 100." });
                }

                var data = await _service.GetTopPerformingEventsAsync(count);

                if (data == null)
                {
                    _logger.LogWarning("Top performing events returned null data for count: {Count}", count);
                    return Json(new { success = false, message = "No event data available." });
                }

                return Json(new { success = true, data });
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument while fetching top performing events. Count: {Count}", count);
                return Json(new { success = false, message = "Invalid parameter provided." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while fetching top performing events. Count: {Count}", count);
                return Json(new { success = false, message = "Database error occurred while retrieving top events." });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while fetching top performing events. Count: {Count}", count);
                return Json(new { success = false, message = "Invalid operation occurred while retrieving top events." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while fetching top performing events. Count: {Count}", count);
                return Json(new { success = false, message = "An unexpected error occurred while retrieving top events." });
            }
        }
    }
}