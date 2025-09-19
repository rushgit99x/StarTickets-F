using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarTickets.Models
{
    public class UserActivityLog
    {
        [Key]
        public int LogId { get; set; }

        public int? UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Action { get; set; } = string.Empty;

        public string? Details { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
