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
using StarTickets.Services.Interfaces;
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
        private readonly IBookingService _bookingService;

        // Constructor with dependency injection
        public BookingController(ApplicationDbContext context, ILogger<BookingController> logger, IEmailService emailService, IBookingService bookingService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _bookingService = bookingService;
        }

        // GET: Booking/BookTicket/{eventId}
        public async Task<IActionResult> BookTicket(int eventId)
        {
            try
            {
                // GET: Booking/BookTicket/{eventId}
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access attempt to BookTicket for event {EventId}", eventId);
                    return RedirectToAction("Login", "Auth");
                }

                // Prepare booking view model
                var viewModel = await _bookingService.PrepareBookingAsync(eventId, userId.Value);
                if (viewModel == null)
                {
                    _logger.LogWarning("Event {EventId} not found or not available for booking", eventId);
                    TempData["ErrorMessage"] = "Event not found or not available for booking.";
                    return RedirectToAction("Index", "Home");
                }

                // Prevent booking past events
                if (viewModel.Event.EventDate <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Attempted to book past event {EventId}", eventId);
                    TempData["ErrorMessage"] = "This event has already occurred.";
                    return RedirectToAction("Index", "Home");
                }

                return View(viewModel); // Return booking form
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading booking page for event {EventId}", eventId);
                TempData["ErrorMessage"] = "An error occurred while loading the booking page. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Booking/EmailTickets/{bookingId}
        [HttpPost]
        public async Task<IActionResult> EmailTickets(int bookingId)
        {
            try
            {
                // Ensure user is logged in
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized email tickets attempt for booking {BookingId}", bookingId);
                    return Json(new { success = false, message = "Not authenticated" });
                }

                // Retrieve booking with all details
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
                    _logger.LogWarning("Booking {BookingId} not found or not paid for user {UserId}", bookingId, userId.Value);
                    return Json(new { success = false, message = "Booking not found or not paid" });
                }

                // Send e-tickets via email
                await _emailService.SendTicketConfirmationEmailAsync(booking);
                _logger.LogInformation("E-tickets sent successfully to {Email} for booking {BookingReference}", booking.Customer.Email, booking.BookingReference);

                return Json(new
                {
                    success = true,
                    message = $"Tickets have been sent to {booking.Customer.Email}"
                });
            }
            catch (DbUpdateException dbEx)
            {
                // Database-specific error
                _logger.LogError(dbEx, "Database error while emailing tickets for booking {BookingId}", bookingId);
                return Json(new
                {
                    success = false,
                    message = "Database error occurred. Please try again later."
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

        // Overloaded EmailTickets for AJAX requests (without bookingId param)
        public async Task<IActionResult> EmailTickets()
        {
            try
            {
                // This method seems to be called from JavaScript without parameters
                // We need to get the booking ID from the request
                var bookingIdString = Request.Query["bookingId"].FirstOrDefault() ??
                                      Request.Form["bookingId"].FirstOrDefault();

                if (!int.TryParse(bookingIdString, out int bookingId))
                {
                    _logger.LogWarning("Invalid booking ID format: {BookingIdString}", bookingIdString);
                    return Json(new { success = false, message = "Invalid booking ID" });
                }

                return await EmailTickets(bookingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EmailTickets method");
                return Json(new { success = false, message = "An error occurred while processing your request." });
            }
        }

        // POST: Booking/ProcessBooking - Creates booking before payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessBooking(BookTicketViewModel model)
        {
            try
            {
                // Ensure user logged in
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized ProcessBooking attempt for event {EventId}", model.EventId);
                    return RedirectToAction("Login", "Auth");
                }

                // Call service to process booking
                var result = await _bookingService.ProcessBookingAsync(model, userId.Value);
                if (!result.Success)
                {
                    // Booking failed (validation or availability issue)
                    _logger.LogWarning("Booking processing failed for user {UserId}, event {EventId}: {Error}",
                        userId.Value, model.EventId, result.Error);
                    ModelState.AddModelError("", result.Error);
                    await ReloadBookingViewModel(model);
                    return View("BookTicket", model);
                }

                // Booking successful
                _logger.LogInformation("Booking {BookingId} created successfully for user {UserId}", result.BookingId, userId.Value);
                TempData["SuccessMessage"] = $"Booking created successfully! Complete your payment to confirm your tickets.";
                return RedirectToAction("BookingConfirmation", new { bookingId = result.BookingId!.Value });
            }
            catch (DbUpdateException dbEx)
            {
                // DB-specific error
                _logger.LogError(dbEx, "Database error while processing booking for user {UserId}",
                    HttpContext.Session.GetInt32("UserId"));
                ModelState.AddModelError("", "A database error occurred. Please try again.");
                await ReloadBookingViewModel(model);
                return View("BookTicket", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing booking for event {EventId}", model.EventId);
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                await ReloadBookingViewModel(model);
                return View("BookTicket", model);
            }
        }

        // GET: Booking/BookingConfirmation/{bookingId} - Displays booking confirmation details
        public async Task<IActionResult> BookingConfirmation(int bookingId)
        {
            try
            {
                // Verify user is logged in
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access to booking confirmation {BookingId}", bookingId);
                    return RedirectToAction("Login", "Auth");
                }
                // Load booking details
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
                    _logger.LogWarning("Booking {BookingId} not found for user {UserId}", bookingId, userId.Value);
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction("Index", "Home");
                }

                return View(booking);// Render confirmation view
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error loading booking confirmation {BookingId}", bookingId);
                TempData["ErrorMessage"] = "A database error occurred. Please try again.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading booking confirmation {BookingId}", bookingId);
                TempData["ErrorMessage"] = "An error occurred while loading your booking. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Booking/ProcessPayment - Processes payment for a booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(PaymentViewModel paymentModel)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                _logger.LogWarning("Unauthorized payment attempt for booking {BookingId}", paymentModel.BookingId);
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                // Start database transaction
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
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
                        _logger.LogWarning("Booking {BookingId} not found or already processed for user {UserId}",
                            paymentModel.BookingId, userId.Value);
                        TempData["ErrorMessage"] = "Booking not found or already processed.";
                        return RedirectToAction("Index", "Home");
                    }

                    // Validate ticket availability again (final check)
                    foreach (var detail in booking.BookingDetails)
                    {
                        if (detail.TicketCategory.AvailableQuantity < detail.Quantity)
                        {
                            _logger.LogWarning("Insufficient tickets for {CategoryName} in booking {BookingId}",
                                detail.TicketCategory.CategoryName, booking.BookingId);
                            TempData["ErrorMessage"] = $"Sorry, {detail.TicketCategory.CategoryName} tickets are no longer available.";
                            await transaction.RollbackAsync();
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

                        _logger.LogInformation("Payment processed successfully for booking {BookingReference}", booking.BookingReference);

                        // Automatically Send e-tickets via email
                        try
                        {
                            await _emailService.SendTicketConfirmationEmailAsync(booking);
                            TempData["PaymentSuccess"] = "Payment completed successfully! Your tickets have been emailed to you.";
                            _logger.LogInformation("E-tickets sent successfully to {Email} for booking {BookingReference}",
                                booking.Customer.Email, booking.BookingReference);
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "Failed to send e-tickets for booking {BookingReference}", booking.BookingReference);
                            TempData["PaymentSuccess"] = "Payment completed successfully! Your tickets are confirmed, but there was an issue sending the email. You can download them below.";
                        }

                        return RedirectToAction("BookingConfirmation", new { bookingId = booking.BookingId });
                    }
                    else
                    {
                        _logger.LogWarning("Payment failed for booking {BookingId}", paymentModel.BookingId);
                        await transaction.RollbackAsync();
                        TempData["PaymentError"] = "Payment failed. Please check your card details and try again.";
                        return RedirectToAction("BookingConfirmation", new { bookingId = booking.BookingId });
                    }
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error processing payment for booking {BookingId}", paymentModel.BookingId);
                TempData["PaymentError"] = "A database error occurred. Please try again.";
                return RedirectToAction("BookingConfirmation", new { bookingId = paymentModel.BookingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for booking {BookingId}", paymentModel.BookingId);
                TempData["PaymentError"] = "An error occurred while processing your payment. Please try again.";
                return RedirectToAction("BookingConfirmation", new { bookingId = paymentModel.BookingId });
            }
        }

        // POST: Booking/ValidatePromoCode - Validates a promotional code and calculates discount
        [HttpPost]
        public async Task<IActionResult> ValidatePromoCode(string promoCode, decimal totalAmount)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(promoCode))
                {
                    return Json(new { valid = false, message = "Please enter a promo code." });
                }

                if (totalAmount <= 0)
                {
                    return Json(new { valid = false, message = "Invalid total amount." });
                }

                // Check for valid promo code
                var promo = await _context.PromotionalCampaigns
                    .FirstOrDefaultAsync(p => p.DiscountCode == promoCode &&
                                             p.IsActive &&
                                             DateTime.UtcNow >= p.StartDate &&
                                             DateTime.UtcNow <= p.EndDate &&
                                             (p.MaxUsage == null || p.CurrentUsage < p.MaxUsage));

                if (promo == null)
                {
                    _logger.LogInformation("Invalid or expired promo code attempted: {PromoCode}", promoCode);
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

                _logger.LogInformation("Promo code {PromoCode} validated successfully with discount {DiscountAmount}",
                    promoCode, discountAmount);

                return Json(new
                {
                    valid = true,
                    discountAmount = discountAmount,
                    finalAmount = totalAmount - discountAmount,
                    message = $"Promo code applied! You save ${discountAmount:F2}"
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error validating promo code {PromoCode}", promoCode);
                return Json(new { valid = false, message = "Database error occurred. Please try again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating promo code {PromoCode}", promoCode);
                return Json(new { valid = false, message = "Error validating promo code. Please try again." });
            }
        }

        //GET: Booking/GetTicketDataWithQR/{bookingId}
        // Retrieves ticket data including QR codes
        [HttpGet]
        public async Task<IActionResult> GetTicketDataWithQR(int bookingId)
        {
            try
            {
                // Verify user is logged in
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized access to ticket data for booking {BookingId}", bookingId);
                    return Unauthorized();
                }

                var booking = await _context.Bookings
                    .Include(b => b.Event)
                        .ThenInclude(e => e.Venue)
                    .Include(b => b.BookingDetails)
                        .ThenInclude(d => d.TicketCategory)
                    .Include(b => b.BookingDetails)
                        .ThenInclude(d => d.Tickets)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == userId.Value);

                if (booking == null)
                {
                    _logger.LogWarning("Booking {BookingId} not found for user {UserId}", bookingId, userId.Value);
                    return NotFound();
                }

                // Prepare ticket data for response
                var ticketData = new List<object>();

                foreach (var detail in booking.BookingDetails)
                {
                    foreach (var ticket in detail.Tickets)
                    {
                        // Generate QR code as base64 data URL
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

                _logger.LogInformation("Ticket data generated successfully for booking {BookingId}", bookingId);
                return Json(ticketData);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error retrieving ticket data for booking {BookingId}", bookingId);
                return StatusCode(500, new { error = "Database error occurred while retrieving ticket data." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving ticket data for booking {BookingId}", bookingId);
                return StatusCode(500, new { error = "An error occurred while retrieving ticket data." });
            }
        }

        // Helper methods to reload booking view model on error
        private async Task ReloadBookingViewModel(BookTicketViewModel model)
        {
            try
            {
                // Reload event and ticket category dat
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading booking view model for event {EventId}", model.EventId);
                // Set empty collections to prevent null reference errors
                model.TicketCategories = new List<TicketCategory>();
            }
        }

        // Generates a unique booking reference
        private string GenerateBookingReference()
        {
            return "BK" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") +
                   new Random().Next(1000, 9999).ToString();
        }
        // Generates a unique ticket number
        private string GenerateTicketNumber(int bookingId, int bookingDetailId, int ticketSequence)
        {
            return $"TK{bookingId:D6}{bookingDetailId:D3}{ticketSequence:D2}";
        }
        // Generates QR code data
        private string GenerateQRCode(string bookingReference, int ticketSequence)
        {
            return $"{bookingReference}-{ticketSequence:D2}";
        }

        // Generates a unique transaction ID
        private string GenerateTransactionId()
        {
            return "TXN" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") +
                   new Random().Next(10000, 99999).ToString();
        }

        // Determines card type from card number
        private string GetCardType(string cardNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cardNumber))
                    return "Credit Card";

                var cleaned = cardNumber.Replace(" ", "");
                if (cleaned.Length < 4)
                    return "Credit Card";

                var firstFour = cleaned.Substring(0, 4);
                if (firstFour.StartsWith("4")) return "Visa";
                if (firstFour.StartsWith("5") || firstFour.StartsWith("2")) return "MasterCard";
                if (firstFour.StartsWith("3")) return "American Express";
                return "Credit Card";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining card type for card number");
                return "Credit Card";
            }
        }

        // Simulates payment gateway processing
        private async Task<bool> ProcessPaymentGateway(PaymentViewModel paymentModel)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in payment gateway processing");
                return false;
            }
        }

        // Generates QR code as base64 data URL
        private string GenerateQRCodeDataUrl(string qrData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(qrData))
                {
                    _logger.LogWarning("Attempted to generate QR code with null or empty data");
                    return null;
                }

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

        // GET: Booking/DownloadTicket/{ticketNumber}
        // Generates and downloads a ticket as a PDF
        public async Task<IActionResult> DownloadTicket(string ticketNumber)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                {
                    _logger.LogWarning("Unauthorized ticket download attempt for ticket {TicketNumber}", ticketNumber);
                    return RedirectToAction("Login", "Auth");
                }

                if (string.IsNullOrEmpty(ticketNumber))
                {
                    _logger.LogWarning("Download ticket called with null or empty ticket number");
                    return NotFound();
                }

                // Load ticket with related data
                var ticket = await _context.Tickets
                    .Include(t => t.BookingDetail)
                        .ThenInclude(bd => bd.Booking)
                            .ThenInclude(b => b.Event)
                                .ThenInclude(e => e.Venue)
                    .Include(t => t.BookingDetail.TicketCategory)
                    .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber &&
                                              t.BookingDetail.Booking.CustomerId == userId.Value);

                if (ticket == null)
                {
                    _logger.LogWarning("Ticket {TicketNumber} not found for user {UserId}", ticketNumber, userId.Value);
                    return NotFound();
                }

                // Generate PDF
                var pdf = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Margin(40);
                        page.Size(PageSizes.A4);

                        // Add ticket header
                        page.Header().Text($"Ticket: {ticket.TicketNumber}")
                            .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                        // Add ticket details
                        page.Content().PaddingVertical(10).Column(col =>
                        {
                            col.Item().Text($"Booking Reference: {ticket.BookingDetail.Booking.BookingReference}");
                            col.Item().Text($"Event: {ticket.BookingDetail.Booking.Event.EventName}");
                            col.Item().Text($"Category: {ticket.BookingDetail.TicketCategory.CategoryName}");
                            col.Item().Text($"Price: {ticket.BookingDetail.UnitPrice:C}");
                            col.Item().Text($"Date: {ticket.BookingDetail.Booking.Event.EventDate:dd MMM yyyy hh:mm tt}");
                            col.Item().Text($"Venue: {ticket.BookingDetail.Booking.Event.Venue?.VenueName ?? "TBA"}");

                            col.Item().PaddingTop(20).Text(ticket.IsUsed ? "Status: Used" : "Status: Valid")
                                .Bold().FontColor(ticket.IsUsed ? Colors.Red.Medium : Colors.Green.Medium);
                        });
                        // Add footer
                        page.Footer().AlignCenter().Text("Generated by StarTickets")
                            .FontSize(10).Light();
                    });
                });
                // Generate PDF bytes and set file name
                var pdfBytes = pdf.GeneratePdf();
                var fileName = $"{ticket.TicketNumber}.pdf";

                _logger.LogInformation("Ticket PDF generated successfully for ticket {TicketNumber}", ticketNumber);
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error downloading ticket {TicketNumber}", ticketNumber);
                TempData["ErrorMessage"] = "A database error occurred while downloading your ticket.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading ticket {TicketNumber}", ticketNumber);
                TempData["ErrorMessage"] = "An error occurred while downloading your ticket. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Booking/GetTicketData/{bookingId}
        // Alias for GetTicketDataWithQR
        [HttpGet]
        public async Task<IActionResult> GetTicketData(int bookingId)
        {
            return await GetTicketDataWithQR(bookingId);
        }
    }
}