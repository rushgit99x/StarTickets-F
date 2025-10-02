using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StarTickets.Models;
using StarTickets.Models.ViewModels;
using StarTickets.Repositories.Interfaces;
using StarTickets.Services.Interfaces;

namespace StarTickets.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _repo;
        private readonly IEmailService _emailService;
        private readonly ILogger<BookingService> _logger;

        public BookingService(IBookingRepository repo, IEmailService emailService, ILogger<BookingService> logger)
        {
            _repo = repo;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<BookTicketViewModel?> PrepareBookingAsync(int eventId, int userId)
        {
            var eventEntity = await _repo.GetPublishedEventForBookingAsync(eventId);
            if (eventEntity == null) return null;
            var user = await _repo.GetUserByIdAsync(userId);
            return new BookTicketViewModel
            {
                Event = eventEntity,
                TicketCategories = eventEntity.TicketCategories?.ToList() ?? new List<TicketCategory>(),
                CustomerEmail = user?.Email,
                CustomerFirstName = user?.FirstName,
                CustomerLastName = user?.LastName,
                CustomerPhone = user?.PhoneNumber
            };
        }

        public async Task<(bool Success, string? Error, int? BookingId)> ProcessBookingAsync(BookTicketViewModel model, int userId)
        {
            try
            {
                using var tx = await _repo.BeginTransactionAsync();

                var eventEntity = await _repo.GetEventForReloadAsync(model.EventId);
                if (eventEntity == null)
                {
                    return (false, "Event not found.", null);
                }

                decimal totalAmount = 0;
                var bookingDetails = new List<BookingDetail>();

                foreach (var selectedCategory in model.SelectedCategories.Where(sc => sc.Quantity > 0))
                {
                    var ticketCategory = eventEntity.TicketCategories?
                        .FirstOrDefault(tc => tc.TicketCategoryId == selectedCategory.TicketCategoryId);
                    if (ticketCategory == null)
                    {
                        return (false, "Ticket category not found.", null);
                    }
                    if (ticketCategory.AvailableQuantity < selectedCategory.Quantity)
                    {
                        return (false, $"Only {ticketCategory.AvailableQuantity} tickets available for {ticketCategory.CategoryName}.", null);
                    }
                    var bookingDetail = new BookingDetail
                    {
                        TicketCategoryId = selectedCategory.TicketCategoryId,
                        Quantity = selectedCategory.Quantity,
                        UnitPrice = ticketCategory.Price,
                        TotalPrice = ticketCategory.Price * selectedCategory.Quantity
                    };
                    bookingDetails.Add(bookingDetail);
                    totalAmount += bookingDetail.TotalPrice;
                }

                if (bookingDetails.Count == 0)
                {
                }

                decimal discountAmount = 0;
                int? promoId = null;
                if (!string.IsNullOrWhiteSpace(model.PromoCode))
                {
                    var promo = await _repo.GetActivePromoByCodeAsync(model.PromoCode);
                    if (promo != null)
                    {
                        if ((int)promo.DiscountType == (int)DiscountType.Percentage)
                        {
                            discountAmount = totalAmount * (promo.DiscountValue / 100);
                        }
                        else
                        {
                            discountAmount = Math.Min(promo.DiscountValue, totalAmount);
                        }
                        promoId = promo.PromotionalCampaignId;
                    }
                    else
                    {
                        _logger.LogError("Promo code {PromoCode} is invalid or expired.", model.PromoCode);
                    }
                }
                decimal finalAmount = totalAmount - discountAmount;
                
                var booking = new Booking {
                    BookingReference = GenerateBookingReference(),
                    CustomerId = userId,
                    EventId = model.EventId,
                    BookingDate = DateTime.UtcNow,
                    TotalAmount = totalAmount,
                    DiscountAmount = discountAmount,
                    FinalAmount = finalAmount,
                    PaymentStatus = PaymentStatus.Pending,
                    PromoCodeUsed = model.PromoCode,
                    Status = BookingStatus.Active,
                    BookingDetails = bookingDetails,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _repo.AddBookingAsync(booking);

                foreach (var bookingDetail in bookingDetails)
                {
                    bookingDetail.BookingId = booking.BookingId;
                    var newTickets = Enumerable.Range(1, bookingDetail.Quantity).Select(i => new Ticket
                    {
                        BookingDetailId = bookingDetail.BookingDetailId,
                        TicketNumber = GenerateTicketNumber(booking.BookingId, bookingDetail.BookingDetailId, i),
                        QRCode = GenerateQRCode(booking.BookingReference, i),
                        IsUsed = false,
                        CreatedAt = DateTime.UtcNow
                    });
                    _repo.AddTickets(newTickets);
                }

                await _repo.SaveChangesAsync();
                await tx.CommitAsync();

                return (true, null, booking.BookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing booking");
                return (false, "An error occurred while processing your booking. Please try again.", null);
            }
        }

        public async Task<Booking?> GetBookingConfirmationAsync(int bookingId, int userId)
        {
            return await _repo.GetBookingWithTicketsAsync(bookingId, userId);
        }

        public async Task<(bool Success, string? Error)> ProcessPaymentAsync(PaymentViewModel paymentModel, int userId)
        {
            try
            {
                using var tx = await _repo.BeginTransactionAsync();

                var booking = await _repo.GetPendingBookingForPaymentAsync(paymentModel.BookingId, userId);
                if (booking == null)
                {
                    return (false, "Booking not found or already processed.");
                }

                foreach (var detail in booking.BookingDetails)
                {
                    if (detail.TicketCategory.AvailableQuantity < detail.Quantity)
                    {
                        return (false, $"Sorry, {detail.TicketCategory.CategoryName} tickets are no longer available.");
                    }
                }

                bool paymentSuccessful = await ProcessPaymentGateway(paymentModel);
                if (!paymentSuccessful)
                {
                    return (false, "Payment failed. Please check your card details and try again.");
                }

                booking.PaymentStatus = PaymentStatus.Completed;
                booking.PaymentMethod = GetCardType(paymentModel.CardNumber);
                booking.PaymentTransactionId = GenerateTransactionId();
                booking.UpdatedAt = DateTime.UtcNow;

                foreach (var detail in booking.BookingDetails)
                {
                    detail.TicketCategory.AvailableQuantity -= detail.Quantity;
                }

                await _repo.SaveChangesAsync();
                await tx.CommitAsync();

                try
                {
                    await _emailService.SendTicketConfirmationEmailAsync(booking);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send e-tickets for booking {BookingRef}", booking.BookingReference);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for booking {BookingId}", paymentModel.BookingId);
                return (false, "An error occurred while processing your payment. Please try again.");
            }
        }

        public async Task<(bool Success, string Message)> ValidatePromoAsync(string promoCode, decimal totalAmount)
        {
            try
            {
                var promo = await _repo.GetActivePromoByCodeAsync(promoCode);
                if (promo == null)
                {
                    return (false, "Invalid or expired promo code.");
                }
                decimal discountAmount = 0;
                if ((int)promo.DiscountType == (int)DiscountType.Percentage)
                    discountAmount = totalAmount * (promo.DiscountValue / 100);
                else
                    discountAmount = Math.Min(promo.DiscountValue, totalAmount);

                return (true, $"Promo code applied! You save ${discountAmount:F2}");
            }
            catch (Exception)
            {
                return (false, "Error validating promo code.");
            }
        }

        public async Task<List<object>?> GetTicketDataWithQRAsync(int bookingId, int userId)
        {
            var booking = await _repo.GetBookingWithTicketsAsync(bookingId, userId);
            if (booking == null) return null;
            var ticketData = new List<object>();
            foreach (var detail in booking.BookingDetails)
            {
                foreach (var ticket in detail.Tickets)
                {
                    string qr = GenerateQRCodeDataUrl(ticket.QRCode);
                    ticketData.Add(new
                    {
                        ticketNumber = ticket.TicketNumber,
                        qrCode = ticket.QRCode,
                        qrCodeDataUrl = qr,
                        eventName = booking.Event.EventName,
                        category = detail.TicketCategory.CategoryName,
                        date = booking.Event.EventDate.ToString("MMM dd, yyyy h:mm tt"),
                        venue = booking.Event.Venue?.VenueName ?? "TBA",
                        price = detail.UnitPrice.ToString("F2")
                    });
                }
            }
            return ticketData;
        }

        public string GenerateQRCodeDataUrl(string qrData)
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrCodeBytes = qrCode.GetGraphic(20);
                return "data:image/png;base64," + Convert.ToBase64String(qrCodeBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code for data: {QRData}", qrData);
                return null;
            }
        }

        public async Task<(bool Success, string Message)> EmailTicketsAsync(int bookingId, int userId)
        {
            try
            {
                var booking = await _repo.GetCompletedBookingForEmailAsync(bookingId, userId);
                if (booking == null) return (false, "Booking not found or not paid");
                await _emailService.SendTicketConfirmationEmailAsync(booking);
                return (true, $"Tickets have been sent to {booking.Customer.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to email tickets for booking {BookingId}", bookingId);
                return (false, "Failed to send email. Please try again or contact support.");
            }
        }

        public async Task<(bool Success, byte[]? PdfBytes)> DownloadTicketAsync(string ticketNumber, int userId)
        {
            var ticket = await _repo.GetTicketByNumberForUserAsync(ticketNumber, userId);
            if (ticket == null) return (false, null);

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
            var pdfBytes = pdf.GeneratePdf();
            return (true, pdfBytes);
        }

        private string GenerateBookingReference()
        {
            return "BK" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999).ToString();
        }
        private string GenerateTicketNumber(int bookingId, int bookingDetailId, int ticketSequence)
        {
            return $"TK{bookingId:D6}{bookingDetailId:D3}{ticketSequence:D2}";
        }
        private string GenerateQRCode(string bookingReference, int ticketSequence)
        {
            return $"{bookingReference}-{ticketSequence:D2}";
        }
        private string GenerateTransactionId()
        {
            return "TXN" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + new Random().Next(10000, 99999).ToString();
        }
        private string GetCardType(string cardNumber)
        {
            var firstFour = cardNumber.Replace(" ", "").Substring(0, 4);
            if (firstFour.StartsWith("4")) return "Visa";
            if (firstFour.StartsWith("5") || firstFour.StartsWith("2")) return "MasterCard";
            if (firstFour.StartsWith("3")) return "American Express";
            return "Credit Card";
        }
        private async Task<bool> ProcessPaymentGateway(PaymentViewModel paymentModel)
        {
            await Task.Delay(2000);
            var cardNumber = paymentModel.CardNumber.Replace(" ", "");
            var testSuccessCards = new[] { "4111111111111111", "5555555555554444" };
            if (testSuccessCards.Contains(cardNumber)) return true;
            if (cardNumber == "4000000000000002") return false;
            return new Random().NextDouble() > 0.05;
        }
    }
}


