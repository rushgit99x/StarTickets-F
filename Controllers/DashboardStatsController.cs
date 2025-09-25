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

        public DashboardStatsController(ApplicationDbContext context, IDashboardStatsService service)
        {
            _context = context;
            _service = service;
        }

        // GET: API endpoint for dashboard statistics
        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            try { var data = await _service.GetStatsAsync(); return Json(data); } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // GET: API endpoint for event status distribution
        [HttpGet]
        public async Task<IActionResult> GetEventStatusDistribution()
        {
            try { var data = await _service.GetEventStatusDistributionAsync(); return Json(new { success = true, data }); } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // GET: API endpoint for category-wise event distribution
        [HttpGet]
        public async Task<IActionResult> GetCategoryDistribution()
        {
            try { var data = await _service.GetCategoryDistributionAsync(); return Json(new { success = true, data }); } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // GET: API endpoint for top performing events
        [HttpGet]
        public async Task<IActionResult> GetTopPerformingEvents(int count = 5)
        {
            try { var data = await _service.GetTopPerformingEventsAsync(count); return Json(new { success = true, data }); } catch (Exception ex) { return Json(new { success = false, message = ex.Message }); }
        }

        // moved to service
    }
}