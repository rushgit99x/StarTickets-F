using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StarTickets.Data;
using StarTickets.Filters;
using StarTickets.Models;
using StarTickets.Models.ViewModels;
using StarTickets.Services.Interfaces;

namespace StarTickets.Controllers
{
    [RoleAuthorize("1")]
    public class PaymentManagementController : Controller
    {
        private readonly IPaymentManagementService _service;
        private readonly ILogger<PaymentManagementController> _logger;

        public PaymentManagementController(
            IPaymentManagementService service,
            ILogger<PaymentManagementController> logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: PaymentManagement
        // Displays a list of payments with optional filtering
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] PaymentFilterViewModel filters)
        {
            try
            {
                _logger.LogInformation("Loading payment management index with filters");// Log index loading attempt

                // Fetch filtered payment data
                var vm = await _service.GetIndexAsync(filters);

                // Load event options for filter dropdown
                ViewBag.Events = await _service.GetEventOptionsAsync();

                return View(vm); // Render index view with view model
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                _logger.LogError(ex, "Database error occurred while loading payment index");
                TempData["ErrorMessage"] = "A database error occurred. Please try again later.";
                return View(new PaymentIndexViewModel());
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Timeout occurred while loading payment index");
                TempData["ErrorMessage"] = "The request timed out. Please try again.";
                return View(new PaymentIndexViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while loading payment index");
                TempData["ErrorMessage"] = "An unexpected error occurred. Please contact support if the issue persists.";
                return View(new PaymentIndexViewModel());
            }
        }

        // GET: PaymentManagement/ExportPdf
        // Exports filtered payment data to a PDF file
        [HttpGet]
        public async Task<IActionResult> ExportPdf([FromQuery] PaymentFilterViewModel filters)
        {
            try
            {
                _logger.LogInformation("Exporting payments to PDF with filters");// Log PDF export attempt

                // Generate PDF bytes from filtered data
                var bytes = await _service.ExportPdfAsync(filters);

                // Check if PDF data is valid
                if (bytes == null || bytes.Length == 0)
                {
                    _logger.LogWarning("PDF export returned empty result");
                    TempData["ErrorMessage"] = "No data available to export.";
                    return RedirectToAction(nameof(Index), filters);//Redirect to index with filters
                }

                // Generate unique file name for PDF
                var fileName = $"Payments_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                _logger.LogInformation("Successfully exported PDF: {FileName}", fileName);

                // Return PDF file for download
                return File(bytes, "application/pdf", fileName);
            }
            catch (InvalidOperationException ex)
            {
                // Handle invalid operation errors
                _logger.LogError(ex, "Invalid operation while generating PDF");
                TempData["ErrorMessage"] = "Unable to generate PDF with the current data.";
                return RedirectToAction(nameof(Index), filters);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                _logger.LogError(ex, "Database error occurred while exporting PDF");
                TempData["ErrorMessage"] = "A database error occurred while generating the report.";
                return RedirectToAction(nameof(Index), filters);
            }
            catch (TimeoutException ex)
            {
                // Handle timeout errors
                _logger.LogError(ex, "Timeout occurred while generating PDF");
                TempData["ErrorMessage"] = "PDF generation timed out. Please try with fewer filters.";
                return RedirectToAction(nameof(Index), filters);
            }
            catch (OutOfMemoryException ex)
            {
                // Handle memory-related errors
                _logger.LogError(ex, "Out of memory while generating PDF");
                TempData["ErrorMessage"] = "The report is too large to generate. Please apply more filters.";
                return RedirectToAction(nameof(Index), filters);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                _logger.LogError(ex, "Failed to export payments PDF");
                TempData["ErrorMessage"] = "Failed to generate PDF report. Please try again.";
                return RedirectToAction(nameof(Index), filters);
            }
        }
    }
}