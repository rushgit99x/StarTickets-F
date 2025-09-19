using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Models.Configuration;
using StarTickets.Models.ViewModels;
using StarTickets.Services;
using System.Diagnostics;

namespace StarTickets.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly IEmailService _emailService;
        private readonly EmailSettings _emailSettings;

        public HomeController(
            ApplicationDbContext context,
            ILogger<HomeController> logger,
            IEmailService emailService,
            IOptions<EmailSettings> emailSettings
        )
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _emailSettings = emailSettings.Value;
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

            // Get featured events for homepage
            var featuredEvents = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.TicketCategories)
                .Where(e => e.IsActive && e.Status == EventStatus.Published && e.EventDate > DateTime.UtcNow)
                .OrderBy(e => e.EventDate)
                .Take(6)
                .ToListAsync();

            // Date ranges for upcoming sections
            var nowUtc = DateTime.UtcNow;
            var endOfWeekUtc = nowUtc.Date.AddDays(7); // next 7 days
            var startOfThisMonthUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfThisMonthUtc = startOfThisMonthUtc.AddMonths(1);
            var startOfNextMonthUtc = endOfThisMonthUtc;
            var endOfNextMonthUtc = startOfNextMonthUtc.AddMonths(1);

            // This Week: upcoming 7 days
            var thisWeekEvents = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.TicketCategories)
                .Where(e => e.IsActive && e.Status == EventStatus.Published && e.EventDate >= nowUtc && e.EventDate < endOfWeekUtc)
                .OrderBy(e => e.EventDate)
                .Take(8)
                .ToListAsync();

            // This Month: events in current calendar month (from now)
            var thisMonthEvents = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.TicketCategories)
                .Where(e => e.IsActive && e.Status == EventStatus.Published && e.EventDate >= nowUtc && e.EventDate < endOfThisMonthUtc)
                .OrderBy(e => e.EventDate)
                .Take(8)
                .ToListAsync();

            // Next Month: events in next calendar month
            var nextMonthEvents = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.TicketCategories)
                .Where(e => e.IsActive && e.Status == EventStatus.Published && e.EventDate >= startOfNextMonthUtc && e.EventDate < endOfNextMonthUtc)
                .OrderBy(e => e.EventDate)
                .Take(8)
                .ToListAsync();

            // Get event categories
            var categories = await _context.EventCategories
                .Include(c => c.Events!.Where(e => e.IsActive && e.Status == EventStatus.Published))
                .ToListAsync();

            // Get venues for location dropdown
            var venues = await _context.Venues
                .Where(v => v.IsActive)
                .OrderBy(v => v.City)
                .ThenBy(v => v.VenueName)
                .ToListAsync();

            var viewModel = new HomeViewModel
            {
                FeaturedEvents = featuredEvents,
                ThisWeekEvents = thisWeekEvents,
                ThisMonthEvents = thisMonthEvents,
                NextMonthEvents = nextMonthEvents,
                Categories = categories,
                Venues = venues,
                IsAuthenticated = userId.HasValue
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> SearchEvents(string query, int? categoryId, string location, DateTime? date)
        {
            var eventsQuery = _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.TicketCategories)
                .Where(e => e.IsActive && e.Status == EventStatus.Published && e.EventDate > DateTime.UtcNow);

            // Apply search filters
            if (!string.IsNullOrWhiteSpace(query))
            {
                eventsQuery = eventsQuery.Where(e =>
                    e.EventName.Contains(query) ||
                    e.Description!.Contains(query) ||
                    e.BandName!.Contains(query) ||
                    e.Performer!.Contains(query));
            }

            if (categoryId.HasValue && categoryId > 0)
            {
                eventsQuery = eventsQuery.Where(e => e.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                eventsQuery = eventsQuery.Where(e =>
                    e.Venue!.City.Contains(location) ||
                    e.Venue!.VenueName.Contains(location));
            }

            if (date.HasValue)
            {
                var startDate = date.Value.Date;
                var endDate = startDate.AddDays(1);
                eventsQuery = eventsQuery.Where(e => e.EventDate >= startDate && e.EventDate < endDate);
            }

            var events = await eventsQuery
                .OrderBy(e => e.EventDate)
                .Take(20)
                .ToListAsync();

            return Json(new
            {
                success = true,
                events = events.Select(e => new {
                    id = e.EventId,
                    name = e.EventName,
                    description = e.Description,
                    date = e.EventDate.ToString("MMM dd, yyyy"),
                    time = e.EventDate.ToString("hh:mm tt"),
                    venue = e.Venue?.VenueName,
                    city = e.Venue?.City,
                    category = e.Category?.CategoryName,
                    image = e.ImageUrl,
                    minPrice = e.TicketCategories?.Min(tc => tc.Price) ?? 0
                })
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetEventsByCategory(int categoryId)
        {
            var events = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Venue)
                .Include(e => e.TicketCategories)
                .Where(e => e.CategoryId == categoryId && e.IsActive &&
                           e.Status == EventStatus.Published && e.EventDate > DateTime.UtcNow)
                .OrderBy(e => e.EventDate)
                .Take(10)
                .ToListAsync();

            return Json(new
            {
                success = true,
                events = events.Select(e => new {
                    id = e.EventId,
                    name = e.EventName,
                    date = e.EventDate.ToString("MMM dd, yyyy"),
                    time = e.EventDate.ToString("hh:mm tt"),
                    venue = e.Venue?.VenueName,
                    city = e.Venue?.City,
                    image = e.ImageUrl,
                    minPrice = e.TicketCategories?.Min(tc => tc.Price) ?? 0
                })
            });
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
                // Here you would typically save to a newsletter subscribers table
                // For now, we'll just simulate success
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

// View Models
namespace StarTickets.Models.ViewModels
{
    public class HomeViewModel
    {
        public List<Event> FeaturedEvents { get; set; } = new List<Event>();
        public List<Event> ThisWeekEvents { get; set; } = new List<Event>();
        public List<Event> ThisMonthEvents { get; set; } = new List<Event>();
        public List<Event> NextMonthEvents { get; set; } = new List<Event>();
        public List<EventCategory> Categories { get; set; } = new List<EventCategory>();
        public List<Venue> Venues { get; set; } = new List<Venue>();
        public bool IsAuthenticated { get; set; }
    }
}