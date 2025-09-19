using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarTickets.Models
{
    public class TicketCategory
    {
        [Key]
        public int TicketCategoryId { get; set; }

        [Required]
        public int EventId { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required]
        public int TotalQuantity { get; set; }

        [Required]
        public int AvailableQuantity { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("EventId")]
        public virtual Event? Event { get; set; }

        public virtual ICollection<BookingDetail>? BookingDetails { get; set; }
    }
}