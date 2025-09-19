namespace StarTickets.Models.ViewModels
{
    public class DeleteEventViewModel
    {
        public Event Event { get; set; }
        public bool CanDelete { get; set; }
        public bool HasBookings { get; set; }
        public bool HasTicketsSold { get; set; }
        public bool HasCompletedPayments { get; set; }
        public int TotalBookings { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalCapacity { get; set; }
        public int AvailableTickets { get; set; }
    }
}
