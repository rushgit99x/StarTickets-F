using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StarTickets.Data;
using StarTickets.Filters;
using StarTickets.Models;
using StarTickets.Models.ViewModels;

namespace StarTickets.Controllers
{
    [RoleAuthorize("1")]
    public class PaymentManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentManagementController> _logger;

        public PaymentManagementController(ApplicationDbContext context, ILogger<PaymentManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] PaymentFilterViewModel filters)
        {
            var query = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Event)
                .AsQueryable();

            // Default: show only completed payments unless explicitly filtered
            if (!filters.PaymentStatus.HasValue)
            {
                query = query.Where(b => b.PaymentStatus == PaymentStatus.Completed);
            }

            if (filters.FromDate.HasValue)
            {
                var fromUtc = filters.FromDate.Value.Date;
                query = query.Where(b => b.BookingDate >= fromUtc);
            }

            if (filters.ToDate.HasValue)
            {
                var toExclusive = filters.ToDate.Value.Date.AddDays(1);
                query = query.Where(b => b.BookingDate < toExclusive);
            }

            if (filters.PaymentStatus.HasValue)
            {
                query = query.Where(b => b.PaymentStatus == filters.PaymentStatus.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.PaymentMethod))
            {
                var method = filters.PaymentMethod.Trim();
                query = query.Where(b => b.PaymentMethod != null && b.PaymentMethod.Contains(method));
            }

            if (!string.IsNullOrWhiteSpace(filters.CustomerQuery))
            {
                var q = filters.CustomerQuery.Trim().ToLower();
                query = query.Where(b =>
                    (b.Customer != null && (
                        (b.Customer.FirstName + " " + b.Customer.LastName).ToLower().Contains(q) ||
                        b.Customer.Email.ToLower().Contains(q)
                    )) || b.BookingReference.ToLower().Contains(q)
                );
            }

            if (filters.EventId.HasValue)
            {
                query = query.Where(b => b.EventId == filters.EventId.Value);
            }

            var items = await query
                .OrderByDescending(b => b.BookingDate)
                .Select(b => new PaymentTransactionRowViewModel
                {
                    BookingId = b.BookingId,
                    BookingReference = b.BookingReference,
                    BookingDate = b.BookingDate,
                    CustomerName = b.Customer != null ? (b.Customer.FirstName + " " + b.Customer.LastName) : "-",
                    CustomerEmail = b.Customer != null ? b.Customer.Email : "-",
                    EventName = b.Event != null ? b.Event.EventName : "-",
                    TotalAmount = b.TotalAmount,
                    DiscountAmount = b.DiscountAmount,
                    FinalAmount = b.FinalAmount,
                    PaymentStatus = b.PaymentStatus,
                    PaymentMethod = b.PaymentMethod,
                    PaymentTransactionId = b.PaymentTransactionId
                })
                .ToListAsync();

            var vm = new PaymentIndexViewModel
            {
                Filters = filters,
                Transactions = items,
                TotalCount = items.Count,
                TotalGross = items.Sum(i => i.TotalAmount),
                TotalDiscounts = items.Sum(i => i.DiscountAmount),
                TotalNet = items.Sum(i => i.FinalAmount)
            };

            ViewBag.Events = await _context.Events
                .OrderBy(e => e.EventName)
                .Select(e => new { e.EventId, e.EventName })
                .ToListAsync();

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf([FromQuery] PaymentFilterViewModel filters)
        {
            try
            {
                // Reuse the filtering from Index
                var query = _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Event)
                    .AsQueryable();

                if (!filters.PaymentStatus.HasValue)
                {
                    query = query.Where(b => b.PaymentStatus == PaymentStatus.Completed);
                }
                if (filters.FromDate.HasValue)
                {
                    var fromUtc = filters.FromDate.Value.Date;
                    query = query.Where(b => b.BookingDate >= fromUtc);
                }
                if (filters.ToDate.HasValue)
                {
                    var toExclusive = filters.ToDate.Value.Date.AddDays(1);
                    query = query.Where(b => b.BookingDate < toExclusive);
                }
                if (filters.PaymentStatus.HasValue)
                {
                    query = query.Where(b => b.PaymentStatus == filters.PaymentStatus.Value);
                }
                if (!string.IsNullOrWhiteSpace(filters.PaymentMethod))
                {
                    var method = filters.PaymentMethod.Trim();
                    query = query.Where(b => b.PaymentMethod != null && b.PaymentMethod.Contains(method));
                }
                if (!string.IsNullOrWhiteSpace(filters.CustomerQuery))
                {
                    var q = filters.CustomerQuery.Trim().ToLower();
                    query = query.Where(b =>
                        (b.Customer != null && (
                            (b.Customer.FirstName + " " + b.Customer.LastName).ToLower().Contains(q) ||
                            b.Customer.Email.ToLower().Contains(q)
                        )) || b.BookingReference.ToLower().Contains(q)
                    );
                }
                if (filters.EventId.HasValue)
                {
                    query = query.Where(b => b.EventId == filters.EventId.Value);
                }

                var list = await query
                    .OrderByDescending(b => b.BookingDate)
                    .ToListAsync();

                var totalGross = list.Sum(b => b.TotalAmount);
                var totalDiscounts = list.Sum(b => b.DiscountAmount);
                var totalNet = list.Sum(b => b.FinalAmount);

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(30);
                        page.Size(PageSizes.A4);

                        page.Header().Column(col =>
                        {
                            col.Item().Text("StarTickets - Payment Transactions Report").FontSize(18).Bold();
                            var period = (filters.FromDate.HasValue || filters.ToDate.HasValue)
                                ? $"Period: {filters.FromDate:yyyy-MM-dd} to {filters.ToDate:yyyy-MM-dd}"
                                : "All Dates";
                            col.Item().Text(period).FontSize(10).FontColor(Colors.Grey.Darken1);
                        });

                        page.Content().PaddingVertical(10).Column(col =>
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2); // Date
                                    columns.RelativeColumn(3); // Booking Ref
                                    columns.RelativeColumn(4); // Customer
                                    columns.RelativeColumn(3); // Event
                                    columns.RelativeColumn(2); // Method
                                    columns.RelativeColumn(2); // Status
                                    columns.RelativeColumn(2); // Net
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Text("Date");
                                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Text("Booking Ref");
                                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Text("Customer");
                                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Text("Event");
                                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Text("Method");
                                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Text("Status");
                                    header.Cell().Padding(4).Background(Colors.Grey.Lighten2).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Text("Amount");
                                });

                                foreach (var b in list)
                                {
                                    table.Cell().Padding(4).Border(0.5f).BorderColor(Colors.Grey.Lighten3).Text(b.BookingDate.ToString("yyyy-MM-dd HH:mm"));
                                    table.Cell().Padding(4).Border(0.5f).BorderColor(Colors.Grey.Lighten3).Text(b.BookingReference);
                                    table.Cell().Padding(4).Border(0.5f).BorderColor(Colors.Grey.Lighten3).Text(b.Customer != null ? ($"{b.Customer.FirstName} {b.Customer.LastName}\n{b.Customer.Email}") : "-");
                                    table.Cell().Padding(4).Border(0.5f).BorderColor(Colors.Grey.Lighten3).Text(b.Event?.EventName ?? "-");
                                    table.Cell().Padding(4).Border(0.5f).BorderColor(Colors.Grey.Lighten3).Text(b.PaymentMethod ?? "-");
                                    table.Cell().Padding(4).Border(0.5f).BorderColor(Colors.Grey.Lighten3).Text(b.PaymentStatus.ToString());
                                    table.Cell().Padding(4).Border(0.5f).BorderColor(Colors.Grey.Lighten3).Text($"{b.FinalAmount:C}");
                                }
                            });

                            col.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Text("");
                                row.ConstantItem(220).Column(stats =>
                                {
                                    stats.Item().Text($"Total Gross: {totalGross:C}").Bold();
                                    stats.Item().Text($"Total Discounts: {totalDiscounts:C}").Bold();
                                    stats.Item().Text($"Total Net: {totalNet:C}").Bold();
                                });
                            });
                        });

                        page.Footer().AlignCenter().Text($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                });

                var bytes = document.GeneratePdf();
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


