using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarTickets.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        [StringLength(50)]
        public string BookingReference { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int EventId { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal FinalAmount { get; set; }

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        [StringLength(200)]
        public string? PaymentTransactionId { get; set; }

        [StringLength(50)]
        public string? PromoCodeUsed { get; set; }

        public int LoyaltyPointsUsed { get; set; } = 0;

        public int LoyaltyPointsEarned { get; set; } = 0;

        public BookingStatus Status { get; set; } = BookingStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CustomerId")]
        public virtual User? Customer { get; set; }

        [ForeignKey("EventId")]
        public virtual Event? Event { get; set; }

        public virtual ICollection<BookingDetail>? BookingDetails { get; set; }
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }

    public enum BookingStatus
    {
        Active,
        Cancelled
    }
}