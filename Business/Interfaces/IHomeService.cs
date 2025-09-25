using StarTickets.Models.ViewModels;

namespace StarTickets.Services.Interfaces
{
    public interface IHomeService
    {
        Task<HomeViewModel> BuildHomeAsync(int? userId);
        Task<object> SearchEventsAsync(string query, int? categoryId, string location, DateTime? date);
        Task<object> GetEventsByCategoryAsync(int categoryId);
        Task SubscribeAsync(string email);
    }
}


