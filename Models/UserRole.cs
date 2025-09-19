using System.ComponentModel.DataAnnotations;

namespace StarTickets.Models
{
    public class UserRole
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<User>? Users { get; set; }
    }

    // Constants for role names
    public static class RoleConstants
    {
        public const string Admin = "Admin";
        public const string EventOrganizer = "EventOrganizer";
        public const string Customer = "Customer";

        public const int AdminId = 1;
        public const int EventOrganizerId = 2;
        public const int CustomerId = 3;
    }
}