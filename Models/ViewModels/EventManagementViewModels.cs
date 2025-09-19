using System.ComponentModel.DataAnnotations;
using StarTickets.Models;

namespace StarTickets.Models.ViewModels
{
    // Event Management Main View Model
    public class EventManagementViewModel
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

    // Create Event View Model
    public class CreateEventViewModel
    {
        [Required(ErrorMessage = "Event name is required")]
        [StringLength(300, ErrorMessage = "Event name cannot exceed 300 characters")]
        [Display(Name = "Event Name")]
        public string EventName { get; set; } = string.Empty;

        [Display(Name = "Description")]
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

        [Required(ErrorMessage = "Organizer is required")]
        [Display(Name = "Event Organizer")]
        public int OrganizerId { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Event Category")]
        public int CategoryId { get; set; }

        [StringLength(200, ErrorMessage = "Band name cannot exceed 200 characters")]
        [Display(Name = "Band/Artist Name")]
        public string? BandName { get; set; }

        [StringLength(200, ErrorMessage = "Performer name cannot exceed 200 characters")]
        [Display(Name = "Performer")]
        public string? Performer { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters")]
        [Display(Name = "Event Image URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Navigation properties for dropdowns
        public List<EventCategory> Categories { get; set; } = new List<EventCategory>();
        public List<Venue> Venues { get; set; } = new List<Venue>();
        public List<User> Organizers { get; set; } = new List<User>();

        // Ticket Categories
        public List<TicketCategoryViewModel> TicketCategories { get; set; } = new List<TicketCategoryViewModel>();
    }

    // Edit Event View Model
    public class EditEventViewModel
    {
        public int EventId { get; set; }

        [Required(ErrorMessage = "Event name is required")]
        [StringLength(300, ErrorMessage = "Event name cannot exceed 300 characters")]
        [Display(Name = "Event Name")]
        public string EventName { get; set; } = string.Empty;

        [Display(Name = "Description")]
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

        [Required(ErrorMessage = "Organizer is required")]
        [Display(Name = "Event Organizer")]
        public int OrganizerId { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Event Category")]
        public int CategoryId { get; set; }

        [StringLength(200, ErrorMessage = "Band name cannot exceed 200 characters")]
        [Display(Name = "Band/Artist Name")]
        public string? BandName { get; set; }

        [StringLength(200, ErrorMessage = "Performer name cannot exceed 200 characters")]
        [Display(Name = "Performer")]
        public string? Performer { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [StringLength(500, ErrorMessage = "Image URL cannot exceed 500 characters")]
        [Display(Name = "Event Image URL")]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Event Status")]
        public EventStatus Status { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Navigation properties for dropdowns
        public List<EventCategory> Categories { get; set; } = new List<EventCategory>();
        public List<Venue> Venues { get; set; } = new List<Venue>();
        public List<User> Organizers { get; set; } = new List<User>();

        // Ticket Categories
        public List<TicketCategoryViewModel> TicketCategories { get; set; } = new List<TicketCategoryViewModel>();
    }

    // Event Details View Model
    public class EventDetailsViewModel
    {
        public Event Event { get; set; } = new Event();
        public int TotalTicketsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalCapacity { get; set; }
        public double TicketsSoldPercentage { get; set; }
    }

    // Ticket Category View Model
    public class TicketCategoryViewModel
    {
        public int TicketCategoryId { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be between 0.01 and 999,999.99")]
        [Display(Name = "Price")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Total quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Total quantity must be at least 1")]
        [Display(Name = "Total Quantity")]
        public int TotalQuantity { get; set; }

        [Display(Name = "Available Quantity")]
        public int AvailableQuantity { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    // Category Management View Models
    public class CategoryManagementViewModel
    {
        public List<EventCategory> Categories { get; set; } = new List<EventCategory>();
        public string SearchTerm { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalCategories { get; set; }
    }

    public class CreateCategoryViewModel
    {
        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }
    }

    public class EditCategoryViewModel
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }
    }

    public class CategoryDetailsViewModel
    {
        public EventCategory Category { get; set; } = new EventCategory();
        public int TotalEvents { get; set; }
        public int ActiveEvents { get; set; }
        public int UpcomingEvents { get; set; }
        public List<Event> RecentEvents { get; set; } = new List<Event>();
    }

    // Venue Management View Models
    public class VenueManagementViewModel
    {
        public List<Venue> Venues { get; set; } = new List<Venue>();
        public List<string> Cities { get; set; } = new List<string>();
        public string SearchTerm { get; set; } = string.Empty;
        public string CityFilter { get; set; } = string.Empty;
        public bool? ActiveFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalVenues { get; set; }
    }

    public class CreateVenueViewModel
    {
        [Required(ErrorMessage = "Venue name is required")]
        [StringLength(200, ErrorMessage = "Venue name cannot exceed 200 characters")]
        [Display(Name = "Venue Name")]
        public string VenueName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [StringLength(100, ErrorMessage = "City name cannot exceed 100 characters")]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "State name cannot exceed 100 characters")]
        [Display(Name = "State/Province")]
        public string? State { get; set; }

        [Required(ErrorMessage = "Country is required")]
        [StringLength(100, ErrorMessage = "Country name cannot exceed 100 characters")]
        [Display(Name = "Country")]
        public string Country { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        [Required(ErrorMessage = "Capacity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1")]
        [Display(Name = "Capacity")]
        public int Capacity { get; set; }

        [Display(Name = "Facilities")]
        public string? Facilities { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [Display(Name = "Contact Phone")]
        public string? ContactPhone { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        [Display(Name = "Contact Email")]
        public string? ContactEmail { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    public class EditVenueViewModel
    {
        public int VenueId { get; set; }

        [Required(ErrorMessage = "Venue name is required")]
        [StringLength(200, ErrorMessage = "Venue name cannot exceed 200 characters")]
        [Display(Name = "Venue Name")]
        public string VenueName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [StringLength(100, ErrorMessage = "City name cannot exceed 100 characters")]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "State name cannot exceed 100 characters")]
        [Display(Name = "State/Province")]
        public string? State { get; set; }

        [Required(ErrorMessage = "Country is required")]
        [StringLength(100, ErrorMessage = "Country name cannot exceed 100 characters")]
        [Display(Name = "Country")]
        public string Country { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        [Required(ErrorMessage = "Capacity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1")]
        [Display(Name = "Capacity")]
        public int Capacity { get; set; }

        [Display(Name = "Facilities")]
        public string? Facilities { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [Display(Name = "Contact Phone")]
        public string? ContactPhone { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        [Display(Name = "Contact Email")]
        public string? ContactEmail { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }

    public class VenueDetailsViewModel
    {
        public Venue Venue { get; set; } = new Venue();
        public int TotalEvents { get; set; }
        public int UpcomingEvents { get; set; }
        public int PastEvents { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Event> RecentEvents { get; set; } = new List<Event>();
    }
}