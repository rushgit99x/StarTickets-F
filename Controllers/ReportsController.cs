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
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(IReportsService reportsService, ILogger<ReportsController> logger)
        {
            _reportsService = reportsService;
            _logger = logger;
        }

        [RoleAuthorize("1")]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to reports index");
                    return RedirectToAction("Login", "Auth");
                }

                // Validate date range
                if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                {
                    _logger.LogWarning("Invalid date range in reports. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                    TempData["ErrorMessage"] = "Start date cannot be after end date.";
                    return View(new ReportsViewModel());
                }

                var model = await _reportsService.BuildReportsAsync(startDate, endDate);
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while loading reports. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "Database error occurred while loading reports.";
                return View(new ReportsViewModel());
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while loading reports. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "An error occurred while processing the report data.";
                return View(new ReportsViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while loading reports. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "An unexpected error occurred while loading reports.";
                return View(new ReportsViewModel());
            }
        }

        [RoleAuthorize("1")]
        public async Task<IActionResult> DownloadSalesReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to download sales report");
                    return RedirectToAction("Login", "Auth");
                }

                // Validate date range
                if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                {
                    _logger.LogWarning("Invalid date range for sales report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                    TempData["ErrorMessage"] = "Start date cannot be after end date.";
                    return RedirectToAction(nameof(Index), new { startDate, endDate });
                }

                var model = await _reportsService.BuildReportsAsync(startDate, endDate);
                var pdfBytes = await _reportsService.GenerateSalesReportAsync(model);

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    _logger.LogWarning("Sales report generated empty PDF. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                    TempData["ErrorMessage"] = "No data available to generate the report.";
                    return RedirectToAction(nameof(Index), new { startDate, endDate });
                }

                _logger.LogInformation("Sales report downloaded successfully by UserId: {UserId}. StartDate: {StartDate}, EndDate: {EndDate}",
                    userId, startDate, endDate);

                var fileName = $"Sales_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while generating sales report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "Database error occurred while generating the report.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "PDF generation error for sales report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "Error occurred while generating the PDF report.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
            catch (OutOfMemoryException ex)
            {
                _logger.LogError(ex, "Out of memory while generating sales report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "The report is too large to generate. Please try a smaller date range.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while downloading sales report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "An unexpected error occurred while generating the report.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
        }

        [RoleAuthorize("1")]
        public async Task<IActionResult> DownloadUsersReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to download users report");
                    return RedirectToAction("Login", "Auth");
                }

                // Validate date range
                if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                {
                    _logger.LogWarning("Invalid date range for users report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                    TempData["ErrorMessage"] = "Start date cannot be after end date.";
                    return RedirectToAction(nameof(Index), new { startDate, endDate });
                }

                var model = await _reportsService.BuildReportsAsync(startDate, endDate);
                var pdfBytes = await _reportsService.GenerateUsersReportAsync(model);

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    _logger.LogWarning("Users report generated empty PDF. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                    TempData["ErrorMessage"] = "No data available to generate the report.";
                    return RedirectToAction(nameof(Index), new { startDate, endDate });
                }

                _logger.LogInformation("Users report downloaded successfully by UserId: {UserId}. StartDate: {StartDate}, EndDate: {EndDate}",
                    userId, startDate, endDate);

                var fileName = $"Users_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while generating users report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "Database error occurred while generating the report.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "PDF generation error for users report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "Error occurred while generating the PDF report.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
            catch (OutOfMemoryException ex)
            {
                _logger.LogError(ex, "Out of memory while generating users report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "The report is too large to generate. Please try a smaller date range.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while downloading users report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "An unexpected error occurred while generating the report.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
        }

        [RoleAuthorize("1")]
        public async Task<IActionResult> DownloadEventsReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to download events report");
                    return RedirectToAction("Login", "Auth");
                }

                // Validate date range
                if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                {
                    _logger.LogWarning("Invalid date range for events report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                    TempData["ErrorMessage"] = "Start date cannot be after end date.";
                    return RedirectToAction(nameof(Index), new { startDate, endDate });
                }

                var model = await _reportsService.BuildReportsAsync(startDate, endDate);
                var pdfBytes = await _reportsService.GenerateEventsReportAsync(model);

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    _logger.LogWarning("Events report generated empty PDF. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                    TempData["ErrorMessage"] = "No data available to generate the report.";
                    return RedirectToAction(nameof(Index), new { startDate, endDate });
                }

                _logger.LogInformation("Events report downloaded successfully by UserId: {UserId}. StartDate: {StartDate}, EndDate: {EndDate}",
                    userId, startDate, endDate);

                var fileName = $"Events_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while generating events report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "Database error occurred while generating the report.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "PDF generation error for events report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "Error occurred while generating the PDF report.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
            catch (OutOfMemoryException ex)
            {
                _logger.LogError(ex, "Out of memory while generating events report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "The report is too large to generate. Please try a smaller date range.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while downloading events report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "An unexpected error occurred while generating the report.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
        }

        [RoleAuthorize("1")]
        public async Task<IActionResult> DownloadBookingsReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to download bookings report");
                    return RedirectToAction("Login", "Auth");
                }

                // Validate date range
                if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                {
                    _logger.LogWarning("Invalid date range for bookings report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                    TempData["ErrorMessage"] = "Start date cannot be after end date.";
                    return RedirectToAction(nameof(Index), new { startDate, endDate });
                }

                var model = await _reportsService.BuildReportsAsync(startDate, endDate);
                var pdfBytes = await _reportsService.GenerateBookingsReportAsync(model);

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    _logger.LogWarning("Bookings report generated empty PDF. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                    TempData["ErrorMessage"] = "No data available to generate the report.";
                    return RedirectToAction(nameof(Index), new { startDate, endDate });
                }

                _logger.LogInformation("Bookings report downloaded successfully by UserId: {UserId}. StartDate: {StartDate}, EndDate: {EndDate}",
                    userId, startDate, endDate);

                var fileName = $"Bookings_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while generating bookings report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "Database error occurred while generating the report.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "PDF generation error for bookings report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "Error occurred while generating the PDF report.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
            catch (OutOfMemoryException ex)
            {
                _logger.LogError(ex, "Out of memory while generating bookings report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "The report is too large to generate. Please try a smaller date range.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while downloading bookings report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "An unexpected error occurred while generating the report.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
        }

        [RoleAuthorize("1")]
        public async Task<IActionResult> DownloadFullReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to download full report");
                    return RedirectToAction("Login", "Auth");
                }

                // Validate date range
                if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                {
                    _logger.LogWarning("Invalid date range for full report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                    TempData["ErrorMessage"] = "Start date cannot be after end date.";
                    return RedirectToAction(nameof(Index), new { startDate, endDate });
                }

                var model = await _reportsService.BuildReportsAsync(startDate, endDate);
                var pdfBytes = await _reportsService.GenerateFullReportAsync(model);

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    _logger.LogWarning("Full report generated empty PDF. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                    TempData["ErrorMessage"] = "No data available to generate the report.";
                    return RedirectToAction(nameof(Index), new { startDate, endDate });
                }

                _logger.LogInformation("Full report downloaded successfully by UserId: {UserId}. StartDate: {StartDate}, EndDate: {EndDate}",
                    userId, startDate, endDate);

                var fileName = $"Complete_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while generating full report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "Database error occurred while generating the report.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "PDF generation error for full report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "Error occurred while generating the PDF report.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
            catch (OutOfMemoryException ex)
            {
                _logger.LogError(ex, "Out of memory while generating full report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "The report is too large to generate. Please try a smaller date range.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while downloading full report. StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                TempData["ErrorMessage"] = "An unexpected error occurred while generating the report.";
                return RedirectToAction(nameof(Index), new { startDate, endDate });
            }
        }
    }
}