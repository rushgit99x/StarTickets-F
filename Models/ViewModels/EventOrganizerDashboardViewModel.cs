using System.ComponentModel.DataAnnotations;
using StarTickets.Models;

namespace StarTickets.Models.ViewModels
{
    // Dashboard View Model for Event Organizer
    public class EventOrganizerDashboardViewModel
    {
        public int TotalEvents { get; set; }
        public int ActiveEvents { get; set; }
        public int DraftEvents { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Event> RecentEvents { get; set; } = new List<Event>();
        public List<Event> UpcomingEvents { get; set; } = new List<Event>();
    }

    // Event Organizer Events Management
    public class EventOrganizerEventsViewModel
    {
        public List<Event> Events { get; set; } = new List<Event>();
        public List<EventCategory> Categories { get; set; } = new List<EventCategory>();
        public string SearchTerm { get; set; } = string.Empty;
        public int CategoryFilter { get; set; } = 0;
        public EventStatus? StatusFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalEvents { get; set; }
    }

    // Create Event View Model for Event Organizer
    public class CreateEventOrganizerViewModel
    {
        [Required(ErrorMessage = "Event name is required")]
        [StringLength(300, ErrorMessage = "Event name cannot exceed 300 characters")]
        [Display(Name = "Event Name")]
        public string EventName { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Event date is required")]
        [Display(Name = "Event Date")]
        [DataType(DataType.DateTime)]
        public DateTime EventDate { get; set; } = DateTime.Now.AddDays(7);

        [Display(Name = "End Date")]
        [DataType(DataType.DateTime)]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Venue is required")]
        [Display(Name = "Venue")]
        public int VenueId { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Event Category")]
        public int CategoryId { get; set; }

        [StringLength(200, ErrorMessage = "Band name cannot exceed 200 characters")]
        [Display(Name = "Band/Artist Name")]
        public string? BandName { get; set; }

        [StringLength(200, ErrorMessage = "Performer name cannot exceed 200 characters")]
        [Display(Name = "Main Performer")]
        public string? Performer { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters")]
        [Display(Name = "Event Image URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Event is Active")]
        public bool IsActive { get; set; } = true;

        // Hidden field - will be set to current user ID
        public int OrganizerId { get; set; }

        // Navigation properties for dropdowns
        public List<EventCategory> Categories { get; set; } = new List<EventCategory>();
        public List<Venue> Venues { get; set; } = new List<Venue>();

        // Ticket Categories
        public List<TicketCategoryViewModel> TicketCategories { get; set; } = new List<TicketCategoryViewModel>();
    }

    // Edit Event View Model for Event Organizer
    public class EditEventOrganizerViewModel
    {
        public int EventId { get; set; }

        [Required(ErrorMessage = "Event name is required")]
        [StringLength(300, ErrorMessage = "Event name cannot exceed 300 characters")]
        [Display(Name = "Event Name")]
        public string EventName { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Event date is required")]
        [Display(Name = "Event Date")]
        [DataType(DataType.DateTime)]
        public DateTime EventDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.DateTime)]
        public DateTime? EndDate { get; set; }

        [Required(ErrorMessage = "Venue is required")]
        [Display(Name = "Venue")]
        public int VenueId { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Event Category")]
        public int CategoryId { get; set; }

        [StringLength(200, ErrorMessage = "Band name cannot exceed 200 characters")]
        [Display(Name = "Band/Artist Name")]
        public string? BandName { get; set; }

        [StringLength(200, ErrorMessage = "Performer name cannot exceed 200 characters")]
        [Display(Name = "Main Performer")]
        public string? Performer { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters")]
        [Display(Name = "Event Image URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Event is Active")]
        public bool IsActive { get; set; } = true;

        // Navigation properties for dropdowns
        public List<EventCategory> Categories { get; set; } = new List<EventCategory>();
        public List<Venue> Venues { get; set; } = new List<Venue>();

        // Ticket Categories
        public List<TicketCategoryViewModel> TicketCategories { get; set; } = new List<TicketCategoryViewModel>();
    }

    // Event Details View Model for Event Organizer
    public class EventOrganizerDetailsViewModel
    {
        public Event Event { get; set; } = new Event();
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalCapacity { get; set; }
        public double TicketsSoldPercentage { get; set; }
        public List<RecentBooking> RecentBookings { get; set; } = new List<RecentBooking>();
        public Dictionary<string, int> SalesByCategory { get; set; } = new Dictionary<string, int>();
    }

    // Recent Booking Summary
    public class RecentBooking
    {
        public string BookingReference { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int TicketsBooked { get; set; }
        public decimal Amount { get; set; }
        public DateTime BookingDate { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
    }

    // Event Organizer Analytics View Model
    public class EventOrganizerAnalyticsViewModel
    {
        public int OrganizerId { get; set; }
        public DateTime DateFrom { get; set; } = DateTime.Now.AddMonths(-6);
        public DateTime DateTo { get; set; } = DateTime.Now;

        // Overall Statistics
        public int TotalEvents { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageTicketPrice { get; set; }
        public double AverageEventOccupancy { get; set; }

        // Charts Data
        public List<MonthlyRevenueData> MonthlyRevenue { get; set; } = new List<MonthlyRevenueData>();
        public List<CategorySalesData> CategorySales { get; set; } = new List<CategorySalesData>();
        public List<VenuePerformanceData> VenuePerformance { get; set; } = new List<VenuePerformanceData>();

        // Top Performing Events
        public List<Event> TopEvents { get; set; } = new List<Event>();
    }

    // Supporting classes for analytics
    public class MonthlyRevenueData
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int TicketsSold { get; set; }
    }

    public class CategorySalesData
    {
        public string Category { get; set; } = string.Empty;
        public int EventCount { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class VenuePerformanceData
    {
        public string VenueName { get; set; } = string.Empty;
        public int EventCount { get; set; }
        public double AverageOccupancy { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    // Event Organizer Profile View Model
    public class EventOrganizerProfileViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Biography")]
        [StringLength(1000, ErrorMessage = "Biography cannot exceed 1000 characters")]
        public string? Biography { get; set; }

        [Display(Name = "Company/Organization")]
        [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters")]
        public string? Company { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [Display(Name = "Website")]
        public string? Website { get; set; }

        // Statistics (read-only)
        public DateTime MemberSince { get; set; }
        public int TotalEventsOrganized { get; set; }
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageRating { get; set; }
    }
}