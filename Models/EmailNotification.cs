using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarTickets.Models
{
    public class EmailNotification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(255)]
        public string EmailAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public EmailStatus Status { get; set; } = EmailStatus.Pending;

        public DateTime? SentAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }

    public enum EmailStatus
    {
        Pending,
        Sent,
        Failed
    }
}