using StarTickets.Models;

namespace StarTickets.Repositories.Interfaces
{
    public interface ICategoryManagementRepository
    {
        IQueryable<EventCategory> QueryCategoriesWithEvents();
        Task<EventCategory?> FindCategoryAsync(int id);
        Task<bool> CategoryNameExistsAsync(string name, int? excludeId = null);
        Task AddCategoryAsync(EventCategory category);
        Task SaveChangesAsync();
        void RemoveCategory(EventCategory category);
    }
}


