using System.ComponentModel.DataAnnotations;
using StarTickets.Models;

namespace StarTickets.Models.ViewModels
{
    public class PaymentFilterViewModel
    {
        [Display(Name = "From date")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To date")]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Payment status")]
        public PaymentStatus? PaymentStatus { get; set; }

        [Display(Name = "Payment method")]
        public string? PaymentMethod { get; set; }

        [Display(Name = "Customer email/name")]
        public string? CustomerQuery { get; set; }

        [Display(Name = "Event")]
        public int? EventId { get; set; }
    }

    public class PaymentTransactionRowViewModel
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentTransactionId { get; set; }
    }

    public class PaymentIndexViewModel
    {
        public PaymentFilterViewModel Filters { get; set; } = new PaymentFilterViewModel();
        public List<PaymentTransactionRowViewModel> Transactions { get; set; } = new List<PaymentTransactionRowViewModel>();

        public int TotalCount { get; set; }
        public decimal TotalGross { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal TotalNet { get; set; }
    }
}


