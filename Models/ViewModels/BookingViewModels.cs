using System.ComponentModel.DataAnnotations;

namespace StarTickets.Models.ViewModels
{
    public class BookTicketViewModel
    {
        public Event Event { get; set; }
        public List<TicketCategory> TicketCategories { get; set; } = new List<TicketCategory>();
        public List<SelectedTicketCategory> SelectedCategories { get; set; } = new List<SelectedTicketCategory>();

        [Required]
        public int EventId { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string CustomerEmail { get; set; }

        [Required(ErrorMessage = "First name is required")]
        public string CustomerFirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        public string CustomerLastName { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        public string? CustomerPhone { get; set; }

        public string? PromoCode { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }

        [Required(ErrorMessage = "Please accept the terms and conditions")]
        public bool AcceptTerms { get; set; }

        public bool ReceiveUpdates { get; set; } = true;
    }

    public class SelectedTicketCategory
    {
        public int TicketCategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal Price { get; set; }
        public int AvailableQuantity { get; set; }

        [Range(0, 10, ErrorMessage = "Quantity must be between 0 and 10")]
        public int Quantity { get; set; }

        public decimal SubTotal => Price * Quantity;
    }

    public class BookingConfirmationViewModel
    {
        public Booking Booking { get; set; }
        public Event Event { get; set; }
        public List<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();
        public List<Ticket> Tickets { get; set; } = new List<Ticket>();
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
    }

    public class MyBookingsViewModel
    {
        public List<Booking> Bookings { get; set; } = new List<Booking>();
        public string SearchTerm { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalBookings { get; set; }
        public int TotalPages { get; set; }
        public PaymentStatus? StatusFilter { get; set; }
    }

    public class BookingDetailsViewModel
    {
        public Booking Booking { get; set; }
        public Event Event { get; set; }
        public Venue Venue { get; set; }
        public List<BookingDetail> BookingDetails { get; set; } = new List<BookingDetail>();
        public List<Ticket> Tickets { get; set; } = new List<Ticket>();
        public bool CanCancel { get; set; }
    }

    public class AdminBookingListViewModel
    {
        public List<Booking> Bookings { get; set; } = new List<Booking>();
        public string Search { get; set; } = string.Empty;
        public PaymentStatus? PaymentStatusFilter { get; set; }
        public Models.BookingStatus? BookingStatusFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalBookings { get; set; }
        public int TotalPages { get; set; }
    }
}