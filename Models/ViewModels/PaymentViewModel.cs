using System.ComponentModel.DataAnnotations;

namespace StarTickets.Models.ViewModels
{
    public class PaymentViewModel
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Card number is required")]
        [StringLength(19, MinimumLength = 13, ErrorMessage = "Card number must be between 13-19 characters")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Expiry date is required")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Expiry date must be in MM/YY format")]
        public string ExpiryDate { get; set; } = string.Empty;

        [Required(ErrorMessage = "CVV is required")]
        [StringLength(4, MinimumLength = 3, ErrorMessage = "CVV must be 3 or 4 digits")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must contain only numbers")]
        public string CVV { get; set; } = string.Empty;

        [Required(ErrorMessage = "Cardholder name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Cardholder name must be between 2-100 characters")]
        public string CardName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Billing address is required")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Billing address must be between 5-200 characters")]
        public string BillingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "City must be between 2-50 characters")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "ZIP code is required")]
        [StringLength(10, MinimumLength = 3, ErrorMessage = "ZIP code must be between 3-10 characters")]
        [RegularExpression(@"^[\d\-\s]+$", ErrorMessage = "ZIP code can only contain numbers, spaces, and hyphens")]
        public string ZipCode { get; set; } = string.Empty;

        // Optional: Country field if you need international support
        public string? Country { get; set; } = "US";

        // Optional: State field for US addresses
        [StringLength(50)]
        public string? State { get; set; }
    }
}