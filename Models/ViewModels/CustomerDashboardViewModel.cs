using System.ComponentModel.DataAnnotations;

namespace StarTickets.Models.ViewModels
{
    public class CustomerDashboardViewModel
    {
        public DashboardStatsViewModel Stats { get; set; } = new();
        public List<BookingViewModel> RecentBookings { get; set; } = new();
        public List<EventViewModel> UpcomingEvents { get; set; } = new();
        public User Customer { get; set; } = new();
    }

    public class DashboardStatsViewModel
    {
        public int TotalBookings { get; set; }
        public int UpcomingEvents { get; set; }
        public int LoyaltyPoints { get; set; }
        public int EventsToRate { get; set; }
    }

    public class BookingViewModel
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public int TicketQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public int Status { get; set; }
        public DateTime BookingDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public bool CanDownloadTicket => Status == 1; // Confirmed bookings only
        public bool CanRate => EventDate < DateTime.Now && Status == 1;
    }

    public class EventViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string VenueAddress { get; set; } = string.Empty;
        public string BandName { get; set; } = string.Empty;
        public string Performer { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public bool HasBooking { get; set; }
        public string BookingReference { get; set; } = string.Empty;
    }

    public class RateEventViewModel
    {
        [Required]
        public int EventId { get; set; }

        [Required]
        public string EventName { get; set; } = string.Empty;

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Review cannot exceed 1000 characters")]
        public string? Review { get; set; }
    }

    public class UpdateProfileViewModel
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }
    }

    public class SetEventReminderViewModel
    {
        [Required]
        public int EventId { get; set; }

        [Required]
        public DateTime ReminderTime { get; set; }
    }
}
