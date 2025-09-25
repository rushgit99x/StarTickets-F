using StarTickets.Models.ViewModels;

namespace StarTickets.Services.Interfaces
{
    public interface IReviewsManagementService
    {
        Task<ReviewsManagementListViewModel> GetIndexAsync(string search, int? rating, bool? approved, int page, int pageSize);
        Task<(bool Success, string Message)> ApproveAsync(int id);
        Task<(bool Success, string Message)> RejectAsync(int id);
        Task<(bool Success, string Message)> DeleteAsync(int id);
    }
}


