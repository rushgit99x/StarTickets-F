using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder; // QRCoder NuGet package
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using StarTickets.Data;
using StarTickets.Filters;
using StarTickets.Models;
using StarTickets.Models.ViewModels;
using StarTickets.Services;
using System.ComponentModel.DataAnnotations;
using System.Drawing; // System.Drawing.Common NuGet package
using System.Drawing.Imaging; // System.Drawing.Common NuGet package

namespace StarTickets.Controllers
{
    //[RoleAuthorize("3")] // Customer only
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookingController> _logger;
        private readonly IEmailService _emailService;

        public BookingController(ApplicationDbContext context, ILogger<BookingController> logger, IEmailService emailService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        // GET: Booking/BookTicket/5
        public async Task<IActionResult> BookTicket(int eventId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var eventEntity = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.TicketCategories.Where(tc => tc.IsActive))
                .FirstOrDefaultAsync(e => e.EventId == eventId && e.IsActive &&
                                          e.Status == EventStatus.Published);

            if (eventEntity == null)
            {
                TempData["ErrorMessage"] = "Event not found or not available for booking.";
                return RedirectToAction("Index", "Home");
            }

            // Check if event is in the future
            if (eventEntity.EventDate <= DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "This event has already occurred.";
                return RedirectToAction("Index", "Home");
            }

            // Get user information
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId.Value);

            var viewModel = new BookTicketViewModel
            {
                Event = eventEntity,
                TicketCategories = eventEntity.TicketCategories?.ToList() ?? new List<TicketCategory>(),
                CustomerEmail = user?.Email,
                CustomerFirstName = user?.FirstName,
                CustomerLastName = user?.LastName,
                CustomerPhone = user?.PhoneNumber
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EmailTickets(int bookingId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "Not authenticated" });

            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.Event)
                        .ThenInclude(e => e.Venue)
                    .Include(b => b.Event)
                        .ThenInclude(e => e.Category)
                    .Include(b => b.Customer)
                    .Include(b => b.BookingDetails)
                        .ThenInclude(bd => bd.TicketCategory)
                    .Include(b => b.BookingDetails)
                        .ThenInclude(bd => bd.Tickets)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId &&
                                             b.CustomerId == userId.Value &&
                                             b.PaymentStatus == PaymentStatus.Completed);

                if (booking == null)
                {
                    return Json(new { success = false, message = "Booking not found or not paid" });
                }

                await _emailService.SendTicketConfirmationEmailAsync(booking);

                return Json(new
                {
                    success = true,
                    message = $"Tickets have been sent to {booking.Customer.Email}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to email tickets for booking {BookingId}", bookingId);
                return Json(new
                {
                    success = false,
                    message = "Failed to send email. Please try again or contact support."
                });
            }
        }

        public async Task<IActionResult> EmailTickets()
        {
            // This method seems to be called from JavaScript without parameters
            // We need to get the booking ID from the request
            var bookingIdString = Request.Query["bookingId"].FirstOrDefault() ??
                                  Request.Form["bookingId"].FirstOrDefault();

            if (!int.TryParse(bookingIdString, out int bookingId))
            {
                return Json(new { success = false, message = "Invalid booking ID" });
            }

            return await EmailTickets(bookingId);
        }

        // POST: Booking/ProcessBooking - Now creates booking without payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessBooking(BookTicketViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                // Validate event and ticket availability
                var eventEntity = await _context.Events
                    .Include(e => e.TicketCategories)
                    .FirstOrDefaultAsync(e => e.EventId == model.EventId && e.IsActive);

                if (eventEntity == null)
                {
                    ModelState.AddModelError("", "Event not found.");
                    await ReloadBookingViewModel(model);
                    return View("BookTicket", model);
                }

                decimal totalAmount = 0;
                var bookingDetails = new List<BookingDetail>();

                // Process each ticket category
                foreach (var selectedCategory in model.SelectedCategories.Where(sc => sc.Quantity > 0))
                {
                    var ticketCategory = eventEntity.TicketCategories?
                        .FirstOrDefault(tc => tc.TicketCategoryId == selectedCategory.TicketCategoryId);

                    if (ticketCategory == null)
                    {
                        ModelState.AddModelError("", $"Ticket category not found.");
                        await ReloadBookingViewModel(model);
                        return View("BookTicket", model);
                    }

                    // Check availability
                    if (ticketCategory.AvailableQuantity < selectedCategory.Quantity)
                    {
                        ModelState.AddModelError("",
                            $"Only {ticketCategory.AvailableQuantity} tickets available for {ticketCategory.CategoryName}.");
                        await ReloadBookingViewModel(model);
                        return View("BookTicket", model);
                    }

                    // Create booking detail
                    var bookingDetail = new BookingDetail
                    {
                        TicketCategoryId = selectedCategory.TicketCategoryId,
                        Quantity = selectedCategory.Quantity,
                        UnitPrice = ticketCategory.Price,
                        TotalPrice = ticketCategory.Price * selectedCategory.Quantity
                    };

                    bookingDetails.Add(bookingDetail);
                    totalAmount += bookingDetail.TotalPrice;

                    // TEMPORARILY reserve tickets (don't deduct yet)
                    // We'll deduct after successful payment
                }

                if (bookingDetails.Count == 0)
                {
                    ModelState.AddModelError("", "Please select at least one ticket.");
                    await ReloadBookingViewModel(model);
                    return View("BookTicket", model);
                }

                // Apply discount if promo code is provided
                decimal discountAmount = 0;
                int? promoId = null;
                if (!string.IsNullOrWhiteSpace(model.PromoCode))
                {
                    var promo = await _context.PromotionalCampaigns
                        .FirstOrDefaultAsync(p => p.DiscountCode == model.PromoCode &&
                                                 p.IsActive &&
                                                 DateTime.UtcNow >= p.StartDate &&
                                                 DateTime.UtcNow <= p.EndDate &&
                                                 (p.MaxUsage == null || p.CurrentUsage < p.MaxUsage));

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
                        // Don't update usage yet - wait for payment confirmation
                    }
                }

                decimal finalAmount = totalAmount - discountAmount;

                // Create booking with PENDING payment status
                var booking = new Booking
                {
                    BookingReference = GenerateBookingReference(),
                    CustomerId = userId.Value,
                    EventId = model.EventId,
                    BookingDate = DateTime.UtcNow,
                    TotalAmount = totalAmount,
                    DiscountAmount = discountAmount,
                    FinalAmount = finalAmount,
                    PaymentStatus = PaymentStatus.Pending,
                    PromoCodeUsed = model.PromoCode,
                    Status = (Models.BookingStatus)BookingStatus.Active,
                    BookingDetails = bookingDetails,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // Store promo ID in session for later use during payment
                if (promoId.HasValue)
                {
                    HttpContext.Session.SetInt32("PromoId", promoId.Value);
                }

                // Generate placeholder tickets (will be activated after payment)
                foreach (var bookingDetail in bookingDetails)
                {
                    bookingDetail.BookingId = booking.BookingId;

                    for (int i = 0; i < bookingDetail.Quantity; i++)
                    {
                        var ticket = new Ticket
                        {
                            BookingDetailId = bookingDetail.BookingDetailId,
                            TicketNumber = GenerateTicketNumber(booking.BookingId, bookingDetail.BookingDetailId, i + 1),
                            QRCode = GenerateQRCode(booking.BookingReference, i + 1),
                            IsUsed = false,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Tickets.Add(ticket);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"Booking created successfully! Complete your payment to confirm your tickets.";
                return RedirectToAction("BookingConfirmation", new { bookingId = booking.BookingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing booking");
                ModelState.AddModelError("", "An error occurred while processing your booking. Please try again.");
                await ReloadBookingViewModel(model);
                return View("BookTicket", model);
            }
        }

        // GET: Booking/BookingConfirmation/5 - Now handles payment processing
        public async Task<IActionResult> BookingConfirmation(int bookingId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var booking = await _context.Bookings
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue)
                .Include(b => b.Event)
                    .ThenInclude(e => e.Category)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.TicketCategory)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.Tickets)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == userId.Value);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction("Index", "Home");
            }

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(PaymentViewModel paymentModel)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var booking = await _context.Bookings
                    .Include(b => b.Event)
                        .ThenInclude(e => e.Venue)
                    .Include(b => b.Event)
                        .ThenInclude(e => e.Category)
                    .Include(b => b.Customer)  // Include customer information for email
                    .Include(b => b.BookingDetails)
                        .ThenInclude(bd => bd.TicketCategory)
                    .Include(b => b.BookingDetails)
                        .ThenInclude(bd => bd.Tickets)
                    .FirstOrDefaultAsync(b => b.BookingId == paymentModel.BookingId &&
                                             b.CustomerId == userId.Value &&
                                             b.PaymentStatus == PaymentStatus.Pending);

                if (booking == null)
                {
                    TempData["ErrorMessage"] = "Booking not found or already processed.";
                    return RedirectToAction("Index", "Home");
                }

                // Validate ticket availability again (final check)
                foreach (var detail in booking.BookingDetails)
                {
                    if (detail.TicketCategory.AvailableQuantity < detail.Quantity)
                    {
                        TempData["ErrorMessage"] = $"Sorry, {detail.TicketCategory.CategoryName} tickets are no longer available.";
                        return RedirectToAction("BookingConfirmation", new { bookingId = booking.BookingId });
                    }
                }

                // Process payment here (integrate with your payment gateway)
                bool paymentSuccessful = await ProcessPaymentGateway(paymentModel);

                if (paymentSuccessful)
                {
                    // Update booking status
                    booking.PaymentStatus = PaymentStatus.Completed;
                    booking.PaymentMethod = GetCardType(paymentModel.CardNumber);
                    booking.PaymentTransactionId = GenerateTransactionId();
                    booking.UpdatedAt = DateTime.UtcNow;

                    // Now actually deduct the ticket quantities
                    foreach (var detail in booking.BookingDetails)
                    {
                        detail.TicketCategory.AvailableQuantity -= detail.Quantity;
                    }

                    // Update promo code usage if applicable
                    var promoId = HttpContext.Session.GetInt32("PromoId");
                    if (promoId.HasValue && !string.IsNullOrEmpty(booking.PromoCodeUsed))
                    {
                        var promo = await _context.PromotionalCampaigns.FindAsync(promoId.Value);
                        if (promo != null)
                        {
                            promo.CurrentUsage++;
                        }
                        HttpContext.Session.Remove("PromoId");
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // AUTOMATICALLY SEND E-TICKETS VIA EMAIL
                    try
                    {
                        await _emailService.SendTicketConfirmationEmailAsync(booking);
                        TempData["PaymentSuccess"] = "Payment completed successfully! Your tickets have been emailed to you.";
                        _logger.LogInformation($"E-tickets sent successfully to {booking.Customer.Email} for booking {booking.BookingReference}");
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, $"Failed to send e-tickets for booking {booking.BookingReference}");
                        TempData["PaymentSuccess"] = "Payment completed successfully! Your tickets are confirmed, but there was an issue sending the email. You can download them below.";
                    }

                    return RedirectToAction("BookingConfirmation", new { bookingId = booking.BookingId });
                }
                else
                {
                    TempData["PaymentError"] = "Payment failed. Please check your card details and try again.";
                    return RedirectToAction("BookingConfirmation", new { bookingId = booking.BookingId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for booking {BookingId}", paymentModel.BookingId);
                TempData["PaymentError"] = "An error occurred while processing your payment. Please try again.";
                return RedirectToAction("BookingConfirmation", new { bookingId = paymentModel.BookingId });
            }
        }

        // POST: Booking/ValidatePromoCode
        [HttpPost]
        public async Task<IActionResult> ValidatePromoCode(string promoCode, decimal totalAmount)
        {
            try
            {
                var promo = await _context.PromotionalCampaigns
                    .FirstOrDefaultAsync(p => p.DiscountCode == promoCode &&
                                             p.IsActive &&
                                             DateTime.UtcNow >= p.StartDate &&
                                             DateTime.UtcNow <= p.EndDate &&
                                             (p.MaxUsage == null || p.CurrentUsage < p.MaxUsage));

                if (promo == null)
                {
                    return Json(new { valid = false, message = "Invalid or expired promo code." });
                }

                decimal discountAmount = 0;
                if ((int)promo.DiscountType == (int)DiscountType.Percentage)
                {
                    discountAmount = totalAmount * (promo.DiscountValue / 100);
                }
                else
                {
                    discountAmount = Math.Min(promo.DiscountValue, totalAmount);
                }

                return Json(new
                {
                    valid = true,
                    discountAmount = discountAmount,
                    finalAmount = totalAmount - discountAmount,
                    message = $"Promo code applied! You save ${discountAmount:F2}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating promo code");
                return Json(new { valid = false, message = "Error validating promo code." });
            }
        }

        // NEW: Server-side QR Code generation endpoint with proper error handling
        [HttpGet]
        public async Task<IActionResult> GetTicketDataWithQR(int bookingId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized();

            var booking = await _context.Bookings
                .Include(b => b.Event)
                    .ThenInclude(e => e.Venue)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.TicketCategory)
                .Include(b => b.BookingDetails)
                    .ThenInclude(d => d.Tickets)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == userId.Value);

            if (booking == null) return NotFound();

            var ticketData = new List<object>();

            foreach (var detail in booking.BookingDetails)
            {
                foreach (var ticket in detail.Tickets)
                {
                    // Generate QR code as base64 data URL with error handling
                    string qrCodeDataUrl = GenerateQRCodeDataUrl(ticket.QRCode);

                    ticketData.Add(new
                    {
                        ticketNumber = ticket.TicketNumber,
                        qrCode = ticket.QRCode,
                        qrCodeDataUrl = qrCodeDataUrl, // Server-generated QR code data URL
                        eventName = booking.Event.EventName,
                        category = detail.TicketCategory.CategoryName,
                        date = booking.Event.EventDate.ToString("MMM dd, yyyy h:mm tt"),
                        venue = booking.Event.Venue?.VenueName ?? "TBA",
                        price = detail.UnitPrice.ToString("F2")
                    });
                }
            }

            return Json(ticketData);
        }

        // Helper methods
        private async Task ReloadBookingViewModel(BookTicketViewModel model)
        {
            var eventEntity = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.TicketCategories.Where(tc => tc.IsActive))
                .FirstOrDefaultAsync(e => e.EventId == model.EventId);

            if (eventEntity != null)
            {
                model.Event = eventEntity;
                model.TicketCategories = eventEntity.TicketCategories?.ToList() ?? new List<TicketCategory>();
            }
        }

        private string GenerateBookingReference()
        {
            return "BK" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") +
                   new Random().Next(1000, 9999).ToString();
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
            return "TXN" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") +
                   new Random().Next(10000, 99999).ToString();
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
            // Simulate payment processing
            // In a real application, you would integrate with Stripe, PayPal, etc.
            await Task.Delay(2000); // Simulate processing time

            // For demo purposes, randomly succeed/fail based on card number
            var cardNumber = paymentModel.CardNumber.Replace(" ", "");

            // Test card numbers that always succeed
            var testSuccessCards = new[] { "4111111111111111", "5555555555554444" };
            if (testSuccessCards.Contains(cardNumber))
                return true;

            // Test card that always fails
            if (cardNumber == "4000000000000002")
                return false;

            // For other cards, simulate 95% success rate
            return new Random().NextDouble() > 0.05;
        }

        private string GenerateQRCodeDataUrl(string qrData)
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


        public async Task<IActionResult> DownloadTicket(string ticketNumber)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Auth");

            if (string.IsNullOrEmpty(ticketNumber))
                return NotFound();

            var ticket = await _context.Tickets
                .Include(t => t.BookingDetail)
                    .ThenInclude(bd => bd.Booking)
                        .ThenInclude(b => b.Event)
                .Include(t => t.BookingDetail.TicketCategory)
                .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber &&
                                          t.BookingDetail.Booking.CustomerId == userId.Value);

            if (ticket == null)
                return NotFound();

            // Generate PDF
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
            var fileName = $"{ticket.TicketNumber}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // Keeping the original method for backward compatibility
        [HttpGet]
        public async Task<IActionResult> GetTicketData(int bookingId)
        {
            return await GetTicketDataWithQR(bookingId);
        }
    }

    // Enums for booking
    public enum BookingStatus
    {
        Active,
        Cancelled
    }

    public enum DiscountType
    {
        Percentage,
        Fixed
    }

    // Payment ViewModel
    public class PaymentViewModel
    {
        public int BookingId { get; set; }
        public decimal Amount { get; set; }

        [Required]
        public string CardNumber { get; set; }

        [Required]
        public string ExpiryDate { get; set; }

        [Required]
        public string CVV { get; set; }

        [Required]
        public string CardName { get; set; }

        [Required]
        public string BillingAddress { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string ZipCode { get; set; }
    }
}