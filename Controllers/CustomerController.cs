using Microsoft.AspNetCore.Mvc;
using StarTickets.Filters;
using StarTickets.Services;
using StarTickets.Models.ViewModels;
using System.IO.Compression;

namespace StarTickets.Controllers
{
    [RoleAuthorize("3")]
    public class CustomerController : Controller
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var dashboardData = await _customerService.GetDashboardDataAsync(userId.Value);
            return View(dashboardData);
        }

        [HttpGet]
        public async Task<IActionResult> GetBookings(int? year = null, int? status = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "User not authenticated" });

            var bookings = await _customerService.GetCustomerBookingsAsync(userId.Value, year, status);
            return Json(new { success = true, data = bookings });
        }

        [HttpGet]
        public async Task<IActionResult> GetUpcomingEvents()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "User not authenticated" });

            var events = await _customerService.GetCustomerUpcomingEventsAsync(userId.Value);
            return Json(new { success = true, data = events });
        }

        [HttpGet]
        public async Task<IActionResult> GetBookingHistory(int? year = null, int? status = null)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "User not authenticated" });

            var history = await _customerService.GetBookingHistoryAsync(userId.Value, year, status);
            return Json(new { success = true, data = history });
        }

        [HttpGet]
        public async Task<IActionResult> GetEventsToRate()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "User not authenticated" });

            var events = await _customerService.GetEventsToRateAsync(userId.Value);
            return Json(new { success = true, data = events });
        }

        [HttpPost]
        public async Task<IActionResult> RateEvent([FromBody] RateEventViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "User not authenticated" });

            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data provided" });

            var result = await _customerService.RateEventAsync(userId.Value, model);
            if (result)
                return Json(new { success = true, message = "Rating submitted successfully" });
            else
                return Json(new { success = false, message = "Unable to submit rating. Please ensure you have a valid booking for this event." });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "User not authenticated" });

            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data provided" });

            var result = await _customerService.UpdateProfileAsync(userId.Value, model);
            if (result)
            {
                // Update session data
                HttpContext.Session.SetString("FirstName", model.FirstName);
                HttpContext.Session.SetString("LastName", model.LastName);

                return Json(new { success = true, message = "Profile updated successfully" });
            }
            else
                return Json(new { success = false, message = "Unable to update profile" });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadTicket(string ticketNumber)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var ticketPdf = await _customerService.GenerateTicketPdfByTicketNumberAsync(ticketNumber, userId.Value);
            if (ticketPdf == null)
                return NotFound("Ticket not found or access denied");

            return File(ticketPdf, "application/pdf", $"ticket-{ticketNumber}.pdf");
        }

        [HttpPost]
        public async Task<IActionResult> EmailTicket(string ticketNumber)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "User not authenticated" });

            var result = await _customerService.EmailTicketByTicketNumberAsync(ticketNumber, userId.Value);
            if (result)
                return Json(new { success = true, message = "Ticket emailed successfully" });
            else
                return Json(new { success = false, message = "Unable to email ticket" });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAllTickets()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var tickets = await _customerService.GetAllCustomerTicketsAsync(userId.Value);
            if (!tickets.Any())
                return NotFound("No tickets found");

            // Create ZIP file
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                for (int i = 0; i < tickets.Count; i++)
                {
                    var entry = archive.CreateEntry($"ticket-{i + 1}.pdf");
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(tickets[i], 0, tickets[i].Length);
                }
            }

            memoryStream.Position = 0;
            return File(memoryStream.ToArray(), "application/zip", "all-tickets.zip");
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "User not authenticated" });

            var stats = await _customerService.GetDashboardStatsAsync(userId.Value);
            return Json(new { success = true, data = stats });
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "User not authenticated" });

            var profile = await _customerService.GetCustomerProfileAsync(userId.Value);
            if (profile == null)
                return Json(new { success = false, message = "Profile not found" });

            var profileData = new
            {
                firstName = profile.FirstName,
                lastName = profile.LastName,
                email = profile.Email,
                phoneNumber = profile.PhoneNumber,
                dateOfBirth = profile.DateOfBirth?.ToString("yyyy-MM-dd"),
                loyaltyPoints = profile.LoyaltyPoints
            };

            return Json(new { success = true, data = profileData });
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteBooking(string bookingReference)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "User not authenticated" });

            if (string.IsNullOrEmpty(bookingReference))
                return Json(new { success = false, message = "Invalid booking reference" });

            var result = await _customerService.DeleteBookingAsync(bookingReference, userId.Value);
            if (result)
                return Json(new { success = true, message = "Booking deleted successfully" });
            else
                return Json(new { success = false, message = "Unable to delete booking. It may not exist or you may not have permission." });
        }

        [HttpGet]
        public async Task<IActionResult> GetTicketsWithQR()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "User not authenticated" });

            var tickets = await _customerService.GetTicketsWithQRCodesAsync(userId.Value);
            return Json(new { success = true, data = tickets });
        }

        [HttpPost]
        public async Task<IActionResult> SetEventReminder([FromBody] SetEventReminderViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "User not authenticated" });

            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data provided" });

            var result = await _customerService.SetEventReminderAsync(userId.Value, model.EventId, model.ReminderTime);
            if (result)
                return Json(new { success = true, message = "Event reminder set successfully" });
            else
                return Json(new { success = false, message = "Unable to set reminder. Please ensure you have a valid booking for this event." });
        }
    }
}