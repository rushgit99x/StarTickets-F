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

        public PaymentManagementController(IPaymentManagementService service, ILogger<PaymentManagementController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] PaymentFilterViewModel filters)
        {
            var vm = await _service.GetIndexAsync(filters);
            ViewBag.Events = await _service.GetEventOptionsAsync();
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf([FromQuery] PaymentFilterViewModel filters)
        {
            try
            {
                var bytes = await _service.ExportPdfAsync(filters);
                var fileName = $"Payments_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
                return File(bytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export payments PDF");
                TempData["ErrorMessage"] = "Failed to generate PDF report.";
                return RedirectToAction(nameof(Index), filters);
            }
        }
    }
}


