// Models/ViewModels/UserManagementViewModel.cs
using StarTickets.Models;
using System.ComponentModel.DataAnnotations;

namespace StarTickets.Models.ViewModels
{
    public class UserManagementViewModel
    {
        public List<User> Users { get; set; } = new List<User>();
        public List<UserRole> UserRoles { get; set; } = new List<UserRole>();
        public string SearchTerm { get; set; } = string.Empty;
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public int TotalUsers { get; set; }
        public int? RoleFilter { get; set; }
        public bool? ActiveFilter { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Please select a role")]
        [Display(Name = "User Role")]
        public int Role { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; } = false;

        public List<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    public class EditUserViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Please select a role")]
        [Display(Name = "User Role")]
        public int Role { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }

        [Display(Name = "Loyalty Points")]
        public int LoyaltyPoints { get; set; }

        [StringLength(100, ErrorMessage = "Password must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password (leave blank to keep current)")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }

        public List<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    public class UserDetailsViewModel
    {
        public User User { get; set; } = new User();
        public UserBookingStats BookingStats { get; set; } = new UserBookingStats();
    }

    public class UserBookingStats
    {
        public int TotalBookings { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastBookingDate { get; set; }
        public string FavoriteEventCategory { get; set; } = "N/A";
        public int EventsAttended { get; set; }
        public int CancelledBookings { get; set; }
    }

    public class UserStatsViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int AdminUsers { get; set; }
        public int OrganizerUsers { get; set; }
        public int CustomerUsers { get; set; }
        public int RecentUsers { get; set; }
    }
    public class DeleteUserViewModel
    {
        public User User { get; set; } = new User();
        public bool HasBookings { get; set; }
        public bool HasEvents { get; set; }
        public bool CanDelete { get; set; }
        public int BookingCount { get; set; }
        public int EventCount { get; set; }
    }
}