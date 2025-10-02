using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Models.Configuration;
using StarTickets.Models.ViewModels;
using StarTickets.Services;
using StarTickets.Services.Interfaces;
using System.Diagnostics;

namespace StarTickets.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IEmailService _emailService;
        private readonly EmailSettings _emailSettings;
        private readonly IHomeService _homeService;

        public HomeController(
            ApplicationDbContext context,
            ILogger<HomeController> logger,
            IEmailService emailService,
            IOptions<EmailSettings> emailSettings,
            IHomeService homeService
        )
        {
            _logger = logger;
            _emailService = emailService;
            _emailSettings = emailSettings.Value;
            _homeService = homeService;
        }

        public IActionResult About()
        {
            try
            {
                var viewModel = new AboutViewModel();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading About page");
                TempData["ErrorMessage"] = "An error occurred while loading the page. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // If user is authenticated, redirect to appropriate dashboard
                var userId = HttpContext.Session.GetInt32("UserId");
                var userRole = HttpContext.Session.GetInt32("UserRole");

                if (userId.HasValue && userRole.HasValue)
                {
                    switch (userRole.Value)
                    {
                        case 1: // Admin
                            return RedirectToAction("Index", "Admin");
                        case 2: // Event Organizer
                            return RedirectToAction("Index", "EventOrganizer");
                        case 3: // Customer
                            return RedirectToAction("Index", "Customer");
                    }
                }

                var viewModel = await _homeService.BuildHomeAsync(userId);
                return View(viewModel);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while loading home page. UserId: {UserId}", HttpContext.Session.GetInt32("UserId"));
                return View(new HomeViewModel()); // Return empty view model
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid operation while loading home page. UserId: {UserId}", HttpContext.Session.GetInt32("UserId"));
                return View(new HomeViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while loading home page. UserId: {UserId}", HttpContext.Session.GetInt32("UserId"));
                return View(new HomeViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchEvents(string query, int? categoryId, string location, DateTime? date)
        {
            try
            {
                // Validate inputs
                if (!string.IsNullOrWhiteSpace(query) && query.Length > 200)
                {
                    _logger.LogWarning("Search query too long: {QueryLength}", query.Length);
                    return Json(new { success = false, message = "Search query is too long." });
                }

                if (!string.IsNullOrWhiteSpace(location) && location.Length > 100)
                {
                    _logger.LogWarning("Location parameter too long: {LocationLength}", location.Length);
                    return Json(new { success = false, message = "Location parameter is too long." });
                }

                var result = await _homeService.SearchEventsAsync(query, categoryId, location, date);
                return Json(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument in search. Query: {Query}, CategoryId: {CategoryId}, Location: {Location}, Date: {Date}",
                    query, categoryId, location, date);
                return Json(new { success = false, message = "Invalid search parameters provided." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during event search. Query: {Query}, CategoryId: {CategoryId}", query, categoryId);
                return Json(new { success = false, message = "Database error occurred while searching events." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching events. Query: {Query}, CategoryId: {CategoryId}, Location: {Location}, Date: {Date}",
                    query, categoryId, location, date);
                return Json(new { success = false, message = "An error occurred while searching events." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEventsByCategory(int categoryId)
        {
            try
            {
                // Validate categoryId
                if (categoryId <= 0)
                {
                    _logger.LogWarning("Invalid categoryId provided: {CategoryId}", categoryId);
                    return Json(new { success = false, message = "Invalid category ID." });
                }

                var result = await _homeService.GetEventsByCategoryAsync(categoryId);

                if (result == null)
                {
                    _logger.LogWarning("No events found for categoryId: {CategoryId}", categoryId);
                    return Json(new { success = false, message = "No events found for this category." });
                }

                return Json(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid argument for category. CategoryId: {CategoryId}", categoryId);
                return Json(new { success = false, message = "Invalid category ID provided." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error fetching events by category. CategoryId: {CategoryId}", categoryId);
                return Json(new { success = false, message = "Database error occurred while fetching events." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching events by category. CategoryId: {CategoryId}", categoryId);
                return Json(new { success = false, message = "An error occurred while fetching events." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Subscribe(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("Newsletter subscription attempted with empty email");
                    return Json(new { success = false, message = "Email is required" });
                }

                // Basic email validation
                if (!IsValidEmail(email))
                {
                    _logger.LogWarning("Newsletter subscription attempted with invalid email format: {Email}", email);
                    return Json(new { success = false, message = "Please provide a valid email address" });
                }

                await _homeService.SubscribeAsync(email);
                _logger.LogInformation("Newsletter subscription successful: {Email}", email);
                return Json(new { success = true, message = "Successfully subscribed!" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Email already subscribed: {Email}", email);
                return Json(new { success = false, message = "This email is already subscribed." });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during newsletter subscription: {Email}", email);
                return Json(new { success = false, message = "Unable to process subscription. Please try again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to newsletter: {Email}", email);
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        public IActionResult Privacy()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading Privacy page");
                TempData["ErrorMessage"] = "An error occurred while loading the page. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public IActionResult Contact()
        {
            try
            {
                var model = new ContactViewModel();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading Contact page");
                TempData["ErrorMessage"] = "An error occurred while loading the contact form. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Additional validation
                if (string.IsNullOrWhiteSpace(model.Email) || !IsValidEmail(model.Email))
                {
                    ModelState.AddModelError("Email", "Please provide a valid email address.");
                    return View(model);
                }

                var subject = $"New Contact Message: {model.Subject}";
                var body = $@"<p>You have received a new contact message from the website.</p>
                               <p><strong>Name:</strong> {System.Net.WebUtility.HtmlEncode(model.Name)}</p>
                               <p><strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(model.Email)}</p>
                               <p><strong>Reason:</strong> {System.Net.WebUtility.HtmlEncode(model.Reason)}</p>
                               <p><strong>Message:</strong><br/>{System.Net.WebUtility.HtmlEncode(model.Message).Replace("\n", "<br/>")}</p>";

                var destination = string.IsNullOrWhiteSpace(_emailSettings.FromEmail)
                    ? "support@startickets.com"
                    : _emailSettings.FromEmail;

                await _emailService.SendEmailAsync(destination, subject, body);

                _logger.LogInformation("Contact form submitted successfully. Email: {Email}, Subject: {Subject}", model.Email, model.Subject);

                TempData["ContactSuccess"] = "Thank you! Your message has been sent.";
                return RedirectToAction(nameof(Contact));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Email service configuration error while sending contact form. Email: {Email}", model?.Email);
                ModelState.AddModelError(string.Empty, "Email service is not properly configured. Please try again later.");
                return View(model);
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Timeout while sending contact email. Email: {Email}", model?.Email);
                ModelState.AddModelError(string.Empty, "The request timed out. Please try again.");
                return View(model);
            }
            catch (System.Net.Mail.SmtpException ex)
            {
                _logger.LogError(ex, "SMTP error while sending contact email. Email: {Email}", model?.Email);
                ModelState.AddModelError(string.Empty, "Unable to send email at this time. Please try again later.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending contact email. Email: {Email}, Subject: {Subject}", model?.Email, model?.Subject);
                ModelState.AddModelError(string.Empty, "An error occurred while sending your message. Please try again later.");
                return View(model);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            try
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Error action");
                return View(new ErrorViewModel { RequestId = "Unknown" });
            }
        }

        // Helper method for email validation
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}