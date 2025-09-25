using StarTickets.Models.ViewModels;

namespace StarTickets.Services.Interfaces
{
    public interface IPaymentManagementService
    {
        Task<PaymentIndexViewModel> GetIndexAsync(PaymentFilterViewModel filters);
        Task<byte[]> ExportPdfAsync(PaymentFilterViewModel filters);
        Task<List<object>> GetEventOptionsAsync();
    }
}


