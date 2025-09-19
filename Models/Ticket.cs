using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarTickets.Models
{
    public class Ticket
    {
        [Key]
        public int TicketId { get; set; }

        [Required]
        public int BookingDetailId { get; set; }

        [Required]
        [StringLength(100)]
        public string TicketNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string QRCode { get; set; } = string.Empty;

        [StringLength(20)]
        public string? SeatNumber { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime? UsedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("BookingDetailId")]
        public virtual BookingDetail? BookingDetail { get; set; }
    }
}