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
        private readonly ApplicationDbContext _context;
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
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _emailSettings = emailSettings.Value;
            _homeService = homeService;
        }
        public IActionResult About()
        {
            var viewModel = new AboutViewModel();
            // Populate viewModel with data as needed
            return View(viewModel);
        }
        public async Task<IActionResult> Index()
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
        [HttpGet]
        public async Task<IActionResult> SearchEvents(string query, int? categoryId, string location, DateTime? date)
        {
            var result = await _homeService.SearchEventsAsync(query, categoryId, location, date);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetEventsByCategory(int categoryId)
        {
            var result = await _homeService.GetEventsByCategoryAsync(categoryId);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> Subscribe(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { success = false, message = "Email is required" });
            }

            try
            {
                await _homeService.SubscribeAsync(email);
                _logger.LogInformation($"Newsletter subscription: {email}");
                return Json(new { success = true, message = "Successfully subscribed!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to newsletter");
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            var model = new ContactViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
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

                TempData["ContactSuccess"] = "Thank you! Your message has been sent.";
                return RedirectToAction(nameof(Contact));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending contact email");
                ModelState.AddModelError(string.Empty, "An error occurred while sending your message. Please try again later.");
                return View(model);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}