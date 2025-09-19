using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarTickets.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(191)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [Required]
        public int Role { get; set; }

        public bool IsActive { get; set; } = true;

        public bool EmailConfirmed { get; set; } = false;

        public int LoyaltyPoints { get; set; } = 0;

        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(255)]
        public string? ResetToken { get; set; }

        public DateTime? ResetTokenExpiry { get; set; }

        // Navigation property
        [ForeignKey("Role")]
        public virtual UserRole? UserRole { get; set; }

        // Computed property for full name
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}