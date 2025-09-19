using System.ComponentModel.DataAnnotations;

namespace StarTickets.Models
{
    public class Venue
    {
        [Key]
        public int VenueId { get; set; }

        [Required]
        [StringLength(200)]
        public string VenueName { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(100)]
        public string? State { get; set; }

        [Required]
        [StringLength(100)]
        public string Country { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [Required]
        public int Capacity { get; set; }

        public string? Facilities { get; set; }

        [StringLength(20)]
        public string? ContactPhone { get; set; }

        [StringLength(255)]
        public string? ContactEmail { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Event>? Events { get; set; }
    }
}
