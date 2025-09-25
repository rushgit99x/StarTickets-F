using StarTickets.Models;
using StarTickets.Models.ViewModels;
using StarTickets.Repositories.Interfaces;
using StarTickets.Services.Interfaces;

namespace StarTickets.Services
{
    public class HomeService : IHomeService
    {
        private readonly IHomeRepository _homeRepository;
        private readonly IEmailService _emailService;

        public HomeService(IHomeRepository homeRepository, IEmailService emailService)
        {
            _homeRepository = homeRepository;
            _emailService = emailService;
        }

        public async Task<HomeViewModel> BuildHomeAsync(int? userId)
        {
            var featuredEvents = await _homeRepository.GetFeaturedEventsAsync(6);

            var nowUtc = DateTime.UtcNow;
            var endOfWeekUtc = nowUtc.Date.AddDays(7);
            var startOfThisMonthUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfThisMonthUtc = startOfThisMonthUtc.AddMonths(1);
            var startOfNextMonthUtc = endOfThisMonthUtc;
            var endOfNextMonthUtc = startOfNextMonthUtc.AddMonths(1);

            var thisWeekEvents = await _homeRepository.GetEventsThisWeekAsync(nowUtc, endOfWeekUtc, 8);
            var thisMonthEvents = await _homeRepository.GetEventsThisMonthAsync(nowUtc, endOfThisMonthUtc, 8);
            var nextMonthEvents = await _homeRepository.GetEventsNextMonthAsync(startOfNextMonthUtc, endOfNextMonthUtc, 8);

            var categories = await _homeRepository.GetCategoriesWithPublishedEventsAsync();
            var venues = await _homeRepository.GetActiveVenuesAsync();

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

            return viewModel;
        }

        public async Task<object> SearchEventsAsync(string query, int? categoryId, string location, DateTime? date)
        {
            var events = await _homeRepository.SearchEventsAsync(query, categoryId, location, date, 20);
            return new
            {
                success = true,
                events = events.Select(e => new
                {
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
            };
        }

        public async Task<object> GetEventsByCategoryAsync(int categoryId)
        {
            var events = await _homeRepository.GetEventsByCategoryAsync(categoryId, 10);
            return new
            {
                success = true,
                events = events.Select(e => new
                {
                    id = e.EventId,
                    name = e.EventName,
                    date = e.EventDate.ToString("MMM dd, yyyy"),
                    time = e.EventDate.ToString("hh:mm tt"),
                    venue = e.Venue?.VenueName,
                    city = e.Venue?.City,
                    image = e.ImageUrl,
                    minPrice = e.TicketCategories?.Min(tc => tc.Price) ?? 0
                })
            };
        }

        public async Task SubscribeAsync(string email)
        {
            // Simulate storing subscription, and send optional confirmation email in the future
            await Task.CompletedTask;
        }
    }
}


