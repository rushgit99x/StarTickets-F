using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Net.Mail;
using System.Net;
using StarTickets.Services.Interfaces;

namespace StarTickets.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public CustomerService(ApplicationDbContext context, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<CustomerDashboardViewModel> GetDashboardDataAsync(int customerId)
        {
            var customer = await GetCustomerProfileAsync(customerId);
            if (customer == null)
                throw new InvalidOperationException("Customer not found");

            var viewModel = new CustomerDashboardViewModel
            {
                Customer = customer,
                Stats = await GetDashboardStatsAsync(customerId),
                RecentBookings = await GetRecentBookingsAsync(customerId, 5),
                UpcomingEvents = await GetCustomerUpcomingEventsAsync(customerId)
            };

            return viewModel;
        }

        public async Task<DashboardStatsViewModel> GetDashboardStatsAsync(int customerId)
        {
            var totalBookings = await _context.Bookings
                .Where(b => b.CustomerId == customerId)
                .CountAsync();

            var upcomingEvents = await _context.Bookings
                .Where(b => b.CustomerId == customerId &&
                           (int)b.Status == 0 && // Active status
                           b.Event!.EventDate > DateTime.Now)
                .CountAsync();

            var loyaltyPoints = await _context.Users
                .Where(u => u.UserId == customerId)
                .Select(u => u.LoyaltyPoints)
                .FirstOrDefaultAsync();

            var eventsToRate = await _context.Bookings
                .Where(b => b.CustomerId == customerId &&
                           (int)b.Status == 0 && // Active status
                           b.Event!.EventDate < DateTime.Now)
                .CountAsync(b => !_context.EventRatings
                    .Any(er => er.EventId == b.EventId && er.CustomerId == customerId));

            return new DashboardStatsViewModel
            {
                TotalBookings = totalBookings,
                UpcomingEvents = upcomingEvents,
                LoyaltyPoints = loyaltyPoints,
                EventsToRate = eventsToRate
            };
        }

        public async Task<List<BookingViewModel>> GetCustomerBookingsAsync(int customerId, int? year = null, int? status = null)
        {
            var query = _context.Bookings
                .Include(b => b.Event)
                    .ThenInclude(e => e!.Venue)
                .Include(b => b.BookingDetails)
                .Where(b => b.CustomerId == customerId);

            if (year.HasValue)
            {
                query = query.Where(b => b.BookingDate.Year == year.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(b => (int)b.Status == status.Value);
            }

            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return bookings.Select(b => new BookingViewModel
            {
                BookingId = b.BookingId,
                BookingReference = b.BookingReference,
                EventName = b.Event?.EventName ?? "Unknown Event",
                EventDate = b.Event?.EventDate ?? DateTime.MinValue,
                VenueName = b.Event?.Venue?.VenueName ?? "Unknown Venue",
                TicketQuantity = b.BookingDetails?.Sum(bd => bd.Quantity) ?? 0,
                TotalAmount = b.FinalAmount,
                Status = (int)b.Status,
                BookingDate = b.BookingDate,
                PaymentMethod = b.PaymentMethod ?? "Unknown"
            }).ToList();
        }

        public async Task<List<EventViewModel>> GetCustomerUpcomingEventsAsync(int customerId)
        {
            var upcomingEvents = await _context.Bookings
                .Include(b => b.Event)
                    .ThenInclude(e => e!.Venue)
                .Include(b => b.Event)
                    .ThenInclude(e => e!.Category)
                .Where(b => b.CustomerId == customerId &&
                            (int)b.Status == 0 && // Active status
                           b.Event!.EventDate > DateTime.Now)
                .OrderBy(b => b.Event!.EventDate)
                .ToListAsync();

            return upcomingEvents.Select(b => new EventViewModel
            {
                EventId = b.Event!.EventId,
                EventName = b.Event.EventName,
                Description = b.Event.Description ?? "",
                EventDate = b.Event.EventDate,
                EndDate = b.Event.EndDate,
                VenueName = b.Event.Venue?.VenueName ?? "Unknown Venue",
                VenueAddress = b.Event.Venue?.Address ?? "",
                BandName = b.Event.BandName ?? "",
                Performer = b.Event.Performer ?? "",
                ImageUrl = b.Event.ImageUrl ?? "",
                CategoryName = b.Event.Category?.CategoryName ?? "",
                HasBooking = true,
                BookingReference = b.BookingReference
            }).ToList();
        }

        public async Task<List<BookingViewModel>> GetBookingHistoryAsync(int customerId, int? year = null, int? status = null)
        {
            return await GetCustomerBookingsAsync(customerId, year, status);
        }

        public async Task<List<EventViewModel>> GetEventsToRateAsync(int customerId)
        {
            var eventsToRate = await _context.Bookings
                .Include(b => b.Event)
                    .ThenInclude(e => e!.Venue)
                .Include(b => b.Event)
                    .ThenInclude(e => e!.Category)
                .Where(b => b.CustomerId == customerId &&
                            (int)b.Status == 0 && // Active status
                           b.Event!.EventDate < DateTime.Now)
                .Where(b => !_context.EventRatings
                    .Any(er => er.EventId == b.EventId && er.CustomerId == customerId))
                .OrderByDescending(b => b.Event!.EventDate)
                .ToListAsync();

            return eventsToRate.Select(b => new EventViewModel
            {
                EventId = b.Event!.EventId,
                EventName = b.Event.EventName,
                Description = b.Event.Description ?? "",
                EventDate = b.Event.EventDate,
                EndDate = b.Event.EndDate,
                VenueName = b.Event.Venue?.VenueName ?? "Unknown Venue",
                VenueAddress = b.Event.Venue?.Address ?? "",
                BandName = b.Event.BandName ?? "",
                Performer = b.Event.Performer ?? "",
                ImageUrl = b.Event.ImageUrl ?? "",
                CategoryName = b.Event.Category?.CategoryName ?? "",
                HasBooking = true,
                BookingReference = b.BookingReference
            }).ToList();
        }

        public async Task<bool> RateEventAsync(int customerId, RateEventViewModel model)
        {
            var hasBooking = await _context.Bookings
                .AnyAsync(b => b.CustomerId == customerId &&
                              b.EventId == model.EventId &&
                              (int)b.Status == 0 &&
                              b.Event!.EventDate < DateTime.Now);

            if (!hasBooking)
                return false;

            var existingRating = await _context.EventRatings
                .FirstOrDefaultAsync(er => er.EventId == model.EventId && er.CustomerId == customerId);

            if (existingRating != null)
            {
                existingRating.Rating = model.Rating;
                existingRating.Review = model.Review;
            }
            else
            {
                var rating = new EventRating
                {
                    EventId = model.EventId,
                    CustomerId = customerId,
                    Rating = model.Rating,
                    Review = model.Review,
                    IsApproved = false,
                    CreatedAt = DateTime.Now
                };

                _context.EventRatings.Add(rating);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateProfileAsync(int customerId, UpdateProfileViewModel model)
        {
            var customer = await _context.Users.FindAsync(customerId);
            if (customer == null)
                return false;

            customer.FirstName = model.FirstName;
            customer.LastName = model.LastName;
            customer.PhoneNumber = model.PhoneNumber;
            customer.DateOfBirth = model.DateOfBirth;
            customer.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<User?> GetCustomerProfileAsync(int customerId)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == customerId && u.Role == RoleConstants.CustomerId);
        }

        public async Task<byte[]?> GenerateTicketPdfAsync(string bookingReference, int customerId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event)
                    .ThenInclude(e => e!.Venue)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd!.Tickets)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.BookingReference == bookingReference &&
                                         b.CustomerId == customerId &&
                                         (int)b.Status == 0);

            if (booking == null)
                return null;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("StarTickets E-Ticket")
                        .SemiBold().FontSize(24).FontColor(Colors.Blue.Medium)
                        .AlignCenter();

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(20);

                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("Booking Reference:")
                                        .SemiBold().FontSize(16);
                                    col.Item().Text(booking.BookingReference)
                                        .FontSize(14).FontColor(Colors.Grey.Darken2);
                                });
                            });

                            column.Item().Element(container =>
                            {
                                container
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(15)
                                    .Column(eventColumn =>
                                    {
                                        eventColumn.Item().Text("Event Details")
                                            .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

                                        eventColumn.Item().PaddingTop(10).Row(row =>
                                        {
                                            row.RelativeItem().Column(col =>
                                            {
                                                col.Item().Text($"Event: {booking.Event?.EventName}")
                                                    .FontSize(14);
                                                col.Item().Text($"Date: {booking.Event?.EventDate:dddd, MMMM dd, yyyy}")
                                                    .FontSize(14);
                                                col.Item().Text($"Time: {booking.Event?.EventDate:HH:mm}")
                                                    .FontSize(14);
                                                col.Item().Text($"Venue: {booking.Event?.Venue?.VenueName}")
                                                    .FontSize(14);
                                                col.Item().Text($"Address: {booking.Event?.Venue?.Address}")
                                                    .FontSize(14);
                                            });
                                        });
                                    });
                            });

                            column.Item().Element(container =>
                            {
                                container
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(15)
                                    .Column(customerColumn =>
                                    {
                                        customerColumn.Item().Text("Customer Details")
                                            .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

                                        customerColumn.Item().PaddingTop(10).Column(col =>
                                        {
                                            col.Item().Text($"Name: {booking.Customer?.FullName}")
                                                .FontSize(14);
                                            col.Item().Text($"Email: {booking.Customer?.Email}")
                                                .FontSize(14);
                                            if (!string.IsNullOrEmpty(booking.Customer?.PhoneNumber))
                                            {
                                                col.Item().Text($"Phone: {booking.Customer.PhoneNumber}")
                                                    .FontSize(14);
                                            }
                                        });
                                    });
                            });

                            if (booking.BookingDetails != null && booking.BookingDetails.Any())
                            {
                                column.Item().Element(container =>
                                {
                                    container
                                        .Border(1)
                                        .BorderColor(Colors.Grey.Lighten2)
                                        .Padding(15)
                                        .Column(ticketColumn =>
                                        {
                                            ticketColumn.Item().Text("Ticket Details")
                                                .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

                                            ticketColumn.Item().PaddingTop(10).Column(col =>
                                            {
                                                foreach (var detail in booking.BookingDetails)
                                                {
                                                    col.Item().PaddingBottom(5).Text($"Category: {detail.TicketCategory?.CategoryName} - Quantity: {detail.Quantity}")
                                                        .FontSize(14);

                                                    if (detail.Tickets != null && detail.Tickets.Any())
                                                    {
                                                        foreach (var ticket in detail.Tickets)
                                                        {
                                                            col.Item().PaddingLeft(20).Text($"• Ticket: {ticket.TicketNumber}")
                                                                .FontSize(12).FontColor(Colors.Grey.Darken1);
                                                        }
                                                    }
                                                }
                                            });
                                        });
                                });
                            }

                            column.Item().Element(container =>
                            {
                                container
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Background(Colors.Grey.Lighten4)
                                    .Padding(15)
                                    .Row(row =>
                                    {
                                        row.RelativeItem().Text($"Total Amount: ${booking.FinalAmount:F2}")
                                            .SemiBold().FontSize(16);
                                        row.RelativeItem().Text($"Payment Method: {booking.PaymentMethod ?? "N/A"}")
                                            .FontSize(14).AlignRight();
                                    });
                            });

                            column.Item().AlignCenter().Element(container =>
                            {
                                container
                                    .Border(1)
                                    .BorderColor(Colors.Grey.Medium)
                                    .Width(150)
                                    .Height(150)
                                    .AlignCenter()
                                    .AlignMiddle()
                                    .Text($"QR Code\n{booking.BookingReference}")
                                    .FontSize(10)
                                    .AlignCenter();
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Generated on: ").FontSize(10);
                            text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).FontSize(10).SemiBold();
                            text.EmptyLine();
                            text.Span("Please present this e-ticket at the venue entrance").FontSize(10).FontColor(Colors.Grey.Darken1);
                        });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<bool> EmailTicketAsync(string bookingReference, int customerId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Event)
                .FirstOrDefaultAsync(b => b.BookingReference == bookingReference &&
                                         b.CustomerId == customerId);

            if (booking?.Customer == null)
                return false;

            var ticketPdf = await GenerateTicketPdfAsync(bookingReference, customerId);
            if (ticketPdf == null)
                return false;

            try
            {
                using var client = new SmtpClient(_configuration["Email:SmtpHost"],
                    int.Parse(_configuration["Email:SmtpPort"] ?? "587"));
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(
                    _configuration["Email:Username"],
                    _configuration["Email:Password"]);

                var message = new MailMessage();
                message.From = new MailAddress(_configuration["Email:FromAddress"] ?? "noreply@startickets.com");
                message.To.Add(booking.Customer.Email);
                message.Subject = $"Your E-Tickets for {booking.Event?.EventName}";
                message.Body = $"Dear {booking.Customer.FullName},\n\nPlease find your e-tickets attached.\n\nThank you for choosing StarTickets!";

                using var pdfStream = new MemoryStream(ticketPdf);
                message.Attachments.Add(new Attachment(pdfStream, $"ticket-{bookingReference}.pdf", "application/pdf"));

                await client.SendMailAsync(message);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<byte[]>> GetAllCustomerTicketsAsync(int customerId)
        {
            var bookings = await _context.Bookings
                .Where(b => b.CustomerId == customerId && (int)b.Status == 0)
                .ToListAsync();

            var tickets = new List<byte[]>();
            foreach (var booking in bookings)
            {
                var ticket = await GenerateTicketPdfAsync(booking.BookingReference, customerId);
                if (ticket != null)
                    tickets.Add(ticket);
            }

            return tickets;
        }

        private async Task<List<BookingViewModel>> GetRecentBookingsAsync(int customerId, int count)
        {
            var recentBookings = await _context.Bookings
                .Include(b => b.Event)
                    .ThenInclude(e => e!.Venue)
                .Include(b => b.BookingDetails)
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.BookingDate)
                .Take(count)
                .ToListAsync();

            return recentBookings.Select(b => new BookingViewModel
            {
                BookingId = b.BookingId,
                BookingReference = b.BookingReference,
                EventName = b.Event?.EventName ?? "Unknown Event",
                EventDate = b.Event?.EventDate ?? DateTime.MinValue,
                VenueName = b.Event?.Venue?.VenueName ?? "Unknown Venue",
                TicketQuantity = b.BookingDetails?.Sum(bd => bd.Quantity) ?? 0,
                TotalAmount = b.FinalAmount,
                Status = (int)b.Status,
                BookingDate = b.BookingDate,
                PaymentMethod = b.PaymentMethod ?? "Unknown"
            }).ToList();
        }

        public async Task<bool> DeleteBookingAsync(string bookingReference, int userId)
        {
            if (string.IsNullOrEmpty(bookingReference))
            {
                return false;
            }

            try
            {
                var booking = await _context.Bookings
                    .FirstOrDefaultAsync(b => b.BookingReference == bookingReference && b.CustomerId == userId);

                if (booking == null)
                {
                    return false;
                }

                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<object>> GetTicketsWithQRCodesAsync(int customerId)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Event)
                    .ThenInclude(e => e!.Venue)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.TicketCategory)
                .Include(b => b.BookingDetails)
                    .ThenInclude(bd => bd.Tickets)
                .Where(b => b.CustomerId == customerId && 
                           b.PaymentStatus == PaymentStatus.Completed &&
                           b.Status == BookingStatus.Active)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            var ticketData = new List<object>();

            foreach (var booking in bookings)
            {
                foreach (var detail in booking.BookingDetails)
                {
                    foreach (var ticket in detail.Tickets)
                    {
                        string qrCodeDataUrl = GenerateQRCodeDataUrl(ticket.QRCode);

                        ticketData.Add(new
                        {
                            ticketNumber = ticket.TicketNumber,
                            qrCode = ticket.QRCode,
                            qrCodeDataUrl = qrCodeDataUrl,
                            eventName = booking.Event?.EventName ?? "Unknown Event",
                            categoryName = detail.TicketCategory?.CategoryName ?? "General",
                            eventDate = booking.Event?.EventDate ?? DateTime.MinValue,
                            venueName = booking.Event?.Venue?.VenueName ?? "TBA",
                            price = detail.UnitPrice.ToString("F2"),
                            bookingReference = booking.BookingReference,
                            isUsed = ticket.IsUsed,
                            bookingId = booking.BookingId
                        });
                    }
                }
            }

            return ticketData;
        }

        public async Task<byte[]?> GenerateTicketPdfByTicketNumberAsync(string ticketNumber, int customerId)
        {
            try
            {
                var ticket = await _context.Tickets
                    .Include(t => t.BookingDetail)
                        .ThenInclude(bd => bd.Booking)
                            .ThenInclude(b => b.Event)
                                .ThenInclude(e => e.Venue)
                    .Include(t => t.BookingDetail)
                        .ThenInclude(bd => bd.TicketCategory)
                    .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber && 
                                           t.BookingDetail.Booking.CustomerId == customerId);

                if (ticket == null) return null;

                var pdf = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(40);
                        page.Size(PageSizes.A4);

                        page.Header().Text($"Ticket: {ticket.TicketNumber}")
                            .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                        page.Content().PaddingVertical(10).Column(col =>
                        {
                            col.Item().Text($"Booking Reference: {ticket.BookingDetail.Booking.BookingReference}");
                            col.Item().Text($"Event: {ticket.BookingDetail.Booking.Event.EventName}");
                            col.Item().Text($"Category: {ticket.BookingDetail.TicketCategory.CategoryName}");
                            col.Item().Text($"Price: {ticket.BookingDetail.UnitPrice:C}");
                            col.Item().Text($"Date: {ticket.BookingDetail.Booking.Event.EventDate:dd MMM yyyy hh:mm tt}");
                            col.Item().Text($"Venue: {ticket.BookingDetail.Booking.Event.Venue?.VenueName}");

                            col.Item().PaddingTop(20).Text(ticket.IsUsed ? "Status: Used" : "Status: Valid")
                                .Bold().FontColor(ticket.IsUsed ? Colors.Red.Medium : Colors.Green.Medium);
                        });

                        page.Footer().AlignCenter().Text("Generated by StarTickets")
                            .FontSize(10).Light();
                    });
                });

                return pdf.GeneratePdf();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> EmailTicketByTicketNumberAsync(string ticketNumber, int customerId)
        {
            try
            {
                var ticket = await _context.Tickets
                    .Include(t => t.BookingDetail)
                        .ThenInclude(bd => bd.Booking)
                            .ThenInclude(b => b.Event)
                                .ThenInclude(e => e.Venue)
                    .Include(t => t.BookingDetail)
                        .ThenInclude(bd => bd.TicketCategory)
                    .Include(t => t.BookingDetail)
                        .ThenInclude(bd => bd.Booking)
                            .ThenInclude(b => b.Customer)
                    .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber && 
                                           t.BookingDetail.Booking.CustomerId == customerId);

                if (ticket == null) return false;

                await _context.Entry(ticket.BookingDetail).Reference(bd => bd.Booking).LoadAsync();
                await _context.Entry(ticket.BookingDetail.Booking).Reference(b => b.Event).LoadAsync();
                await _context.Entry(ticket.BookingDetail.Booking).Reference(b => b.Customer).LoadAsync();
                await _context.Entry(ticket.BookingDetail).Reference(bd => bd.TicketCategory).LoadAsync();

                await _emailService.SendTicketEmailAsync(ticket, ticket.BookingDetail.Booking);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private string GenerateQRCodeDataUrl(string qrData)
        {
            try
            {
                using var qrGenerator = new QRCoder.QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCoder.QRCodeGenerator.ECCLevel.Q);
                var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
                byte[] qrCodeBytes = qrCode.GetGraphic(20);
                return "data:image/png;base64," + Convert.ToBase64String(qrCodeBytes);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> SetEventReminderAsync(int customerId, int eventId, DateTime reminderTime)
        {
            try
            {
                var hasBooking = await _context.Bookings
                    .AnyAsync(b => b.CustomerId == customerId &&
                                  b.EventId == eventId &&
                                  (int)b.Status == 0 &&
                                  b.Event!.EventDate > DateTime.Now);

                if (!hasBooking)
                    return false;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<EventViewModel>> GetUpcomingEventsForRemindersAsync()
        {
            var upcomingEvents = await _context.Bookings
                .Include(b => b.Event)
                    .ThenInclude(e => e!.Venue)
                .Include(b => b.Event)
                    .ThenInclude(e => e!.Category)
                .Include(b => b.Customer)
                .Where(b => (int)b.Status == 0 &&
                           b.Event!.EventDate > DateTime.Now &&
                           b.Event.EventDate <= DateTime.Now.AddDays(1))
                .OrderBy(b => b.Event!.EventDate)
                .ToListAsync();

            return upcomingEvents.Select(b => new EventViewModel
            {
                EventId = b.Event!.EventId,
                EventName = b.Event.EventName,
                Description = b.Event.Description ?? "",
                EventDate = b.Event.EventDate,
                EndDate = b.Event.EndDate,
                VenueName = b.Event.Venue?.VenueName ?? "Unknown Venue",
                VenueAddress = b.Event.Venue?.Address ?? "",
                BandName = b.Event.BandName ?? "",
                Performer = b.Event.Performer ?? "",
                ImageUrl = b.Event.ImageUrl ?? "",
                CategoryName = b.Event.Category?.CategoryName ?? "",
                HasBooking = true,
                BookingReference = b.BookingReference
            }).ToList();
        }
    }
}
