using System.Collections.Generic;

namespace StarTickets.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public string AdminFullName { get; set; } = string.Empty;
        public string AdminEmail { get; set; } = string.Empty;
        public string AdminInitials { get; set; } = string.Empty;
        
        public int TotalUsers { get; set; }
        public int ActiveEvents { get; set; }
        public int TicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }

        public List<RecentEventViewModel> RecentEvents { get; set; } = new();
        public List<decimal> MonthlyRevenue { get; set; } = new();
        public List<string> MonthlyLabels { get; set; } = new();
    }

    public class RecentEventViewModel
    {
        public string EventName { get; set; } = string.Empty;
        public string OrganizerName { get; set; } = string.Empty;
        public System.DateTime EventDate { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}


