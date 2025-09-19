using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarTickets.Models
{
    public class EventRating
    {
        [Key]
        public int RatingId { get; set; }

        [Required]
        public int EventId { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Review { get; set; }

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("EventId")]
        public virtual Event? Event { get; set; }

        [ForeignKey("CustomerId")]
        public virtual User? Customer { get; set; }
    }
}
