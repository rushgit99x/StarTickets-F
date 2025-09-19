using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Sockets;

namespace StarTickets.Models
{
    public class BookingDetail
    {
        [Key]
        public int BookingDetailId { get; set; }

        [Required]
        public int BookingId { get; set; }

        [Required]
        public int TicketCategoryId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        // Navigation properties
        [ForeignKey("BookingId")]
        public virtual Booking? Booking { get; set; }

        [ForeignKey("TicketCategoryId")]
        public virtual TicketCategory? TicketCategory { get; set; }

        public virtual ICollection<Ticket>? Tickets { get; set; }
    }
}
