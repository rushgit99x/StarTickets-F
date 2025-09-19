using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarTickets.Models
{
    public class SystemNotification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; } = NotificationType.Info;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}