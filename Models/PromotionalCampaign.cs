using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarTickets.Models
{
    public class PromotionalCampaign
    {
        [Key]
        public int CampaignId { get; set; }

        [Required]
        [StringLength(200)]
        public string CampaignName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? DiscountCode { get; set; }

        [Required]
        public DiscountType DiscountType { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountValue { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public int? MaxUsage { get; set; }

        public int CurrentUsage { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public int? ApplicableEventId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ApplicableEventId")]
        public virtual Event? ApplicableEvent { get; set; }
        public int? PromotionalCampaignId { get; internal set; }
    }

    public enum DiscountType
    {
        Percentage,
        Fixed
    }
}