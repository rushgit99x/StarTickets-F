using System;
using System.Collections.Generic;

namespace StarTickets.Models.ViewModels
{
    public class ReportsFilterViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ReportsKpisViewModel
    {
        public decimal TotalSales { get; set; }
        public int TotalBookings { get; set; }
        public int TotalEvents { get; set; }
        public int TotalUsers { get; set; }

        public decimal SalesToday { get; set; }
        public int BookingsToday { get; set; }
        public int EventsUpcoming { get; set; }
        public int NewUsersThisMonth { get; set; }
    }

    public class SalesByDateItem
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }

    public class TopEventSalesItem
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public int Bookings { get; set; }
    }

    public class ReportsViewModel
    {
        public ReportsFilterViewModel Filter { get; set; } = new ReportsFilterViewModel();
        public ReportsKpisViewModel Kpis { get; set; } = new ReportsKpisViewModel();

        public List<SalesByDateItem> SalesByDate { get; set; } = new List<SalesByDateItem>();
        public List<TopEventSalesItem> TopEvents { get; set; } = new List<TopEventSalesItem>();
    }
}


