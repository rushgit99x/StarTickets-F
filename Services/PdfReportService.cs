using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StarTickets.Models.ViewModels;

namespace StarTickets.Services
{
    public class PdfReportService : IPdfReportService
    {
        public PdfReportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateSalesReportAsync(ReportsViewModel model)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("Sales Report")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Item().Text($"Report Period: {model.Filter.StartDate?.ToString("yyyy-MM-dd")} to {model.Filter.EndDate?.ToString("yyyy-MM-dd")}")
                                .FontSize(10).FontColor(Colors.Grey.Medium);

                            x.Item().PaddingTop(20).Text("Sales Summary").FontSize(16).SemiBold();
                            x.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Text($"Total Sales: {model.Kpis.TotalSales:C}");
                                row.RelativeItem().Text($"Sales Today: {model.Kpis.SalesToday:C}");
                            });

                            x.Item().PaddingTop(20).Text("Sales by Date").FontSize(16).SemiBold();
                            x.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Date").SemiBold();
                                    header.Cell().Text("Amount").SemiBold();
                                });

                                foreach (var item in model.SalesByDate)
                                {
                                    table.Cell().Text(item.Date.ToString("yyyy-MM-dd"));
                                    table.Cell().Text(item.Amount.ToString("C"));
                                }
                            });

                            x.Item().PaddingTop(20).Text("Top Events by Sales").FontSize(16).SemiBold();
                            x.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Event Name").SemiBold();
                                    header.Cell().Text("Bookings").SemiBold();
                                    header.Cell().Text("Sales").SemiBold();
                                });

                                foreach (var ev in model.TopEvents)
                                {
                                    table.Cell().Text(ev.EventName);
                                    table.Cell().Text(ev.Bookings.ToString());
                                    table.Cell().Text(ev.Sales.ToString("C"));
                                }
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateUsersReportAsync(ReportsViewModel model)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("Users Report")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Item().Text($"Report Period: {model.Filter.StartDate?.ToString("yyyy-MM-dd")} to {model.Filter.EndDate?.ToString("yyyy-MM-dd")}")
                                .FontSize(10).FontColor(Colors.Grey.Medium);

                            x.Item().PaddingTop(20).Text("User Statistics").FontSize(16).SemiBold();
                            x.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Text($"Total Users: {model.Kpis.TotalUsers}");
                                row.RelativeItem().Text($"New Users This Month: {model.Kpis.NewUsersThisMonth}");
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateEventsReportAsync(ReportsViewModel model)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("Events Report")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Item().Text($"Report Period: {model.Filter.StartDate?.ToString("yyyy-MM-dd")} to {model.Filter.EndDate?.ToString("yyyy-MM-dd")}")
                                .FontSize(10).FontColor(Colors.Grey.Medium);

                            x.Item().PaddingTop(20).Text("Event Statistics").FontSize(16).SemiBold();
                            x.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Text($"Total Events: {model.Kpis.TotalEvents}");
                                row.RelativeItem().Text($"Upcoming Events: {model.Kpis.EventsUpcoming}");
                            });

                            x.Item().PaddingTop(20).Text("Top Events by Sales").FontSize(16).SemiBold();
                            x.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Event Name").SemiBold();
                                    header.Cell().Text("Bookings").SemiBold();
                                    header.Cell().Text("Sales").SemiBold();
                                });

                                foreach (var ev in model.TopEvents)
                                {
                                    table.Cell().Text(ev.EventName);
                                    table.Cell().Text(ev.Bookings.ToString());
                                    table.Cell().Text(ev.Sales.ToString("C"));
                                }
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateBookingsReportAsync(ReportsViewModel model)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("Bookings Report")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Item().Text($"Report Period: {model.Filter.StartDate?.ToString("yyyy-MM-dd")} to {model.Filter.EndDate?.ToString("yyyy-MM-dd")}")
                                .FontSize(10).FontColor(Colors.Grey.Medium);

                            x.Item().PaddingTop(20).Text("Booking Statistics").FontSize(16).SemiBold();
                            x.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Text($"Total Bookings: {model.Kpis.TotalBookings}");
                                row.RelativeItem().Text($"Bookings Today: {model.Kpis.BookingsToday}");
                            });

                            x.Item().PaddingTop(20).Text("Sales by Date").FontSize(16).SemiBold();
                            x.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Date").SemiBold();
                                    header.Cell().Text("Amount").SemiBold();
                                });

                                foreach (var item in model.SalesByDate)
                                {
                                    table.Cell().Text(item.Date.ToString("yyyy-MM-dd"));
                                    table.Cell().Text(item.Amount.ToString("C"));
                                }
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateFullReportAsync(ReportsViewModel model)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("Complete System Report")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Item().Text($"Report Period: {model.Filter.StartDate?.ToString("yyyy-MM-dd")} to {model.Filter.EndDate?.ToString("yyyy-MM-dd")}")
                                .FontSize(10).FontColor(Colors.Grey.Medium);

                            x.Item().PaddingTop(20).Text("System Overview").FontSize(16).SemiBold();
                            x.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Text($"Total Sales: {model.Kpis.TotalSales:C}");
                                row.RelativeItem().Text($"Total Bookings: {model.Kpis.TotalBookings}");
                            });
                            x.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text($"Total Events: {model.Kpis.TotalEvents}");
                                row.RelativeItem().Text($"Total Users: {model.Kpis.TotalUsers}");
                            });

                            x.Item().PaddingTop(20).Text("Today's Activity").FontSize(16).SemiBold();
                            x.Item().PaddingTop(10).Row(row =>
                            {
                                row.RelativeItem().Text($"Sales Today: {model.Kpis.SalesToday:C}");
                                row.RelativeItem().Text($"Bookings Today: {model.Kpis.BookingsToday}");
                            });
                            x.Item().PaddingTop(5).Row(row =>
                            {
                                row.RelativeItem().Text($"Upcoming Events: {model.Kpis.EventsUpcoming}");
                                row.RelativeItem().Text($"New Users This Month: {model.Kpis.NewUsersThisMonth}");
                            });

                            x.Item().PaddingTop(20).Text("Sales by Date").FontSize(16).SemiBold();
                            x.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Date").SemiBold();
                                    header.Cell().Text("Amount").SemiBold();
                                });

                                foreach (var item in model.SalesByDate)
                                {
                                    table.Cell().Text(item.Date.ToString("yyyy-MM-dd"));
                                    table.Cell().Text(item.Amount.ToString("C"));
                                }
                            });

                            x.Item().PaddingTop(20).Text("Top Events by Sales").FontSize(16).SemiBold();
                            x.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Event Name").SemiBold();
                                    header.Cell().Text("Bookings").SemiBold();
                                    header.Cell().Text("Sales").SemiBold();
                                });

                                foreach (var ev in model.TopEvents)
                                {
                                    table.Cell().Text(ev.EventName);
                                    table.Cell().Text(ev.Bookings.ToString());
                                    table.Cell().Text(ev.Sales.ToString("C"));
                                }
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }
    }
}

