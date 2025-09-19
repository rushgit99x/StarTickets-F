using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarTickets.Models
{
    public class PasswordResetToken
    {
        [Key]
        public int TokenId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(255)]
        public string Token { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}