using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Filters;
using StarTickets.Models.ViewModels;
using StarTickets.Services;
using StarTickets.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StarTickets.Controllers
{
    public class ReportsController : Controller
    {
        private readonly IReportsService _reportsService;

        public ReportsController(IReportsService reportsService)
        {
            _reportsService = reportsService;
        }

        [RoleAuthorize("1")]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var model = await _reportsService.BuildReportsAsync(startDate, endDate);
            return View(model);
        }

        [RoleAuthorize("1")]
        public async Task<IActionResult> DownloadSalesReport(DateTime? startDate, DateTime? endDate)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var model = await _reportsService.BuildReportsAsync(startDate, endDate);
            var pdfBytes = await _reportsService.GenerateSalesReportAsync(model);
            
            var fileName = $"Sales_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [RoleAuthorize("1")]
        public async Task<IActionResult> DownloadUsersReport(DateTime? startDate, DateTime? endDate)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var model = await _reportsService.BuildReportsAsync(startDate, endDate);
            var pdfBytes = await _reportsService.GenerateUsersReportAsync(model);
            
            var fileName = $"Users_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [RoleAuthorize("1")]
        public async Task<IActionResult> DownloadEventsReport(DateTime? startDate, DateTime? endDate)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var model = await _reportsService.BuildReportsAsync(startDate, endDate);
            var pdfBytes = await _reportsService.GenerateEventsReportAsync(model);
            
            var fileName = $"Events_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [RoleAuthorize("1")]
        public async Task<IActionResult> DownloadBookingsReport(DateTime? startDate, DateTime? endDate)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var model = await _reportsService.BuildReportsAsync(startDate, endDate);
            var pdfBytes = await _reportsService.GenerateBookingsReportAsync(model);
            
            var fileName = $"Bookings_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [RoleAuthorize("1")]
        public async Task<IActionResult> DownloadFullReport(DateTime? startDate, DateTime? endDate)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var model = await _reportsService.BuildReportsAsync(startDate, endDate);
            var pdfBytes = await _reportsService.GenerateFullReportAsync(model);
            
            var fileName = $"Complete_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // moved to service layer
    }
}


