using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StarTickets.Models;
using StarTickets.Models.ViewModels;
using StarTickets.Repositories.Interfaces;
using StarTickets.Services.Interfaces;

namespace StarTickets.Services
{
    public class PaymentManagementService : IPaymentManagementService
    {
        private readonly IPaymentManagementRepository _repo;
        private readonly ILogger<PaymentManagementService> _logger;

        public PaymentManagementService(IPaymentManagementRepository repo, ILogger<PaymentManagementService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<PaymentIndexViewModel> GetIndexAsync(PaymentFilterViewModel filters)
        {
            var query = _repo.QueryBookingsWithCustomerAndEvent();
            if (!filters.PaymentStatus.HasValue)
                query = query.Where(b => b.PaymentStatus == PaymentStatus.Completed);
            if (filters.FromDate.HasValue)
                query = query.Where(b => b.BookingDate >= filters.FromDate.Value.Date);
            if (filters.ToDate.HasValue)
                query = query.Where(b => b.BookingDate < filters.ToDate.Value.Date.AddDays(1));
            if (filters.PaymentStatus.HasValue)
                query = query.Where(b => b.PaymentStatus == filters.PaymentStatus.Value);
            if (!string.IsNullOrWhiteSpace(filters.PaymentMethod))
                query = query.Where(b => b.PaymentMethod != null && b.PaymentMethod.Contains(filters.PaymentMethod.Trim()));
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
                query = query.Where(b => b.EventId == filters.EventId.Value);

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

            return new PaymentIndexViewModel
            {
                Filters = filters,
                Transactions = items,
                TotalCount = items.Count,
                TotalGross = items.Sum(i => i.TotalAmount),
                TotalDiscounts = items.Sum(i => i.DiscountAmount),
                TotalNet = items.Sum(i => i.FinalAmount)
            };
        }

        public async Task<byte[]> ExportPdfAsync(PaymentFilterViewModel filters)
        {
            var vm = await GetIndexAsync(filters);
            var list = vm.Transactions;
            var totalGross = vm.TotalGross;
            var totalDiscounts = vm.TotalDiscounts;
            var totalNet = vm.TotalNet;

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
                                table.Cell().Padding(4).Border(0.5f).BorderColor(Colors.Grey.Lighten3).Text($"{b.CustomerName}\n{b.CustomerEmail}");
                                table.Cell().Padding(4).Border(0.5f).BorderColor(Colors.Grey.Lighten3).Text(b.EventName);
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

            return document.GeneratePdf();
        }

        public async Task<List<object>> GetEventOptionsAsync()
        {
            var items = await _repo.GetEventOptionsAsync();
            return items.Select(e => new { e.EventId, e.EventName } as object).ToList();
        }
    }
}