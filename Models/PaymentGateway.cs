using System.ComponentModel.DataAnnotations;

namespace StarTickets.Models
{
    public class PaymentGateway
    {
        [Key]
        public int GatewayId { get; set; }

        [Required]
        [StringLength(100)]
        public string GatewayName { get; set; } = string.Empty;

        [Required]
        public string Configuration { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
