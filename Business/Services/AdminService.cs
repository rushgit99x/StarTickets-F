using StarTickets.Models.ViewModels;
using StarTickets.Repositories.Interfaces;
using StarTickets.Services.Interfaces;

namespace StarTickets.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _adminRepository;

        public AdminService(IAdminRepository adminRepository)
        {
            _adminRepository = adminRepository;
        }

        public AdminDashboardViewModel BuildDashboard(int adminUserId)
        {
            var model = new AdminDashboardViewModel
            {
                TotalUsers = _adminRepository.GetTotalUsers(),
                ActiveEvents = _adminRepository.GetActiveEvents(),
                TicketsSold = _adminRepository.GetTicketsSold(),
                TotalRevenue = _adminRepository.GetTotalRevenue(),
                MonthlyLabels = Enumerable.Range(0, 12)
                    .Select(i => System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(i + 1))
                    .ToList()
            };

            var currentUser = _adminRepository.GetUserById(adminUserId);
            if (currentUser != null)
            {
                model.AdminFullName = (currentUser.FirstName + " " + currentUser.LastName).Trim();
                model.AdminEmail = currentUser.Email;
                var initials = (currentUser.FirstName?.Trim().Length > 0 ? currentUser.FirstName.Trim()[0].ToString() : "") +
                               (currentUser.LastName?.Trim().Length > 0 ? currentUser.LastName.Trim()[0].ToString() : "");
                model.AdminInitials = string.IsNullOrWhiteSpace(initials) ? "AD" : initials.ToUpper();
            }

            var currentYear = DateTime.UtcNow.Year;
            var revenueByMonth = _adminRepository.GetMonthlyRevenueByMonth(currentYear);
            model.MonthlyRevenue = Enumerable.Range(1, 12)
                .Select(m => revenueByMonth.ContainsKey(m) ? revenueByMonth[m] : 0m)
                .ToList();

            var recentEvents = _adminRepository.GetRecentEventsBasic(3);
            var recentEventIds = recentEvents.Select(e => e.EventId).ToList();
            var ticketsByEvent = _adminRepository.GetTicketsSoldByEventIds(recentEventIds);
            var revenueByEvent = _adminRepository.GetRevenueByEventIds(recentEventIds);

            foreach (var e in recentEvents)
            {
                model.RecentEvents.Add(new RecentEventViewModel
                {
                    EventName = e.EventName,
                    OrganizerName = e.OrganizerName,
                    EventDate = e.EventDate,
                    VenueName = e.VenueName,
                    TicketsSold = ticketsByEvent.ContainsKey(e.EventId) ? ticketsByEvent[e.EventId] : 0,
                    Revenue = revenueByEvent.ContainsKey(e.EventId) ? revenueByEvent[e.EventId] : 0m,
                    Status = e.Status.ToString()
                });
            }

            return model;
        }
    }
}


