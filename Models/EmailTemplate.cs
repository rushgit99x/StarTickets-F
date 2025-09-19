using System.ComponentModel.DataAnnotations;

namespace StarTickets.Models
{
    public class EmailTemplate
    {
        [Key]
        public int TemplateId { get; set; }

        [Required]
        [StringLength(100)]
        public string TemplateName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
