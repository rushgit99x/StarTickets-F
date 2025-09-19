using System.ComponentModel.DataAnnotations;

namespace StarTickets.Models
{
    public class SystemSetting
    {
        [Key]
        public int SettingId { get; set; }

        [Required]
        [StringLength(100)]
        public string SettingKey { get; set; } = string.Empty;

        [Required]
        public string SettingValue { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}