using System.ComponentModel.DataAnnotations;

namespace StarTickets.Models.ViewModels
{
    public class ContactViewModel
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(2000, MinimumLength = 10)]
        public string Message { get; set; } = string.Empty;

        public bool Sent { get; set; }
        public string? StatusMessage { get; set; }
    }
}


