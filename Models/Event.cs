using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarTickets.Models
{
    public class Event
    {
        [Key]
        public int EventId { get; set; }

        [Required]
        [StringLength(300)]
        public string EventName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        public int VenueId { get; set; }

        [Required]
        public int OrganizerId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [StringLength(200)]
        public string? BandName { get; set; }

        [StringLength(200)]
        public string? Performer { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public EventStatus Status { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("VenueId")]
        public virtual Venue? Venue { get; set; }

        [ForeignKey("OrganizerId")]
        public virtual User? Organizer { get; set; }

        [ForeignKey("CategoryId")]
        public virtual EventCategory? Category { get; set; }

        public virtual ICollection<TicketCategory>? TicketCategories { get; set; }
        public virtual ICollection<Booking>? Bookings { get; set; }
        public virtual ICollection<EventRating>? Ratings { get; set; }
    }

    public enum EventStatus
    {
        Draft,
        Published,
        Cancelled,
        Completed
    }
}
