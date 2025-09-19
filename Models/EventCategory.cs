using System.ComponentModel.DataAnnotations;

namespace StarTickets.Models
{
    public class EventCategory
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Event>? Events { get; set; }
    }
}
