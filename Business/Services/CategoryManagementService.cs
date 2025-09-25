using Microsoft.EntityFrameworkCore;
using StarTickets.Models;
using StarTickets.Models.ViewModels;
using StarTickets.Repositories.Interfaces;
using StarTickets.Services.Interfaces;

namespace StarTickets.Services
{
    public class CategoryManagementService : ICategoryManagementService
    {
        private readonly ICategoryManagementRepository _repo;
        private readonly ILogger<CategoryManagementService> _logger;

        public CategoryManagementService(ICategoryManagementRepository repo, ILogger<CategoryManagementService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<CategoryManagementViewModel> GetIndexAsync(string searchTerm, int page, int pageSize)
        {
            var query = _repo.QueryCategoriesWithEvents();
            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(c => c.CategoryName.Contains(searchTerm) || c.Description!.Contains(searchTerm));

            var total = await query.CountAsync();
            var categories = await query.OrderBy(c => c.CategoryName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new CategoryManagementViewModel
            {
                Categories = categories,
                SearchTerm = searchTerm,
                CurrentPage = page,
                PageSize = pageSize,
                TotalCategories = total,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            };
        }

        public async Task<bool> CreateAsync(CreateCategoryViewModel model)
        {
            try
            {
                if (await _repo.CategoryNameExistsAsync(model.CategoryName))
                    return false;
                var category = new EventCategory
                {
                    CategoryName = model.CategoryName,
                    Description = model.Description,
                    CreatedAt = DateTime.UtcNow
                };
                await _repo.AddCategoryAsync(category);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return false;
            }
        }

        public async Task<EditCategoryViewModel?> GetEditAsync(int id)
        {
            var category = await _repo.FindCategoryAsync(id);
            if (category == null) return null;
            return new EditCategoryViewModel
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                Description = category.Description
            };
        }

        public async Task<bool> EditAsync(EditCategoryViewModel model)
        {
            try
            {
                var category = await _repo.FindCategoryAsync(model.CategoryId);
                if (category == null) return false;
                if (await _repo.CategoryNameExistsAsync(model.CategoryName, model.CategoryId)) return false;
                category.CategoryName = model.CategoryName;
                category.Description = model.Description;
                await _repo.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                return false;
            }
        }

        public async Task<CategoryDetailsViewModel?> GetDetailsAsync(int id)
        {
            var category = await _repo.FindCategoryAsync(id);
            if (category == null) return null;
            var totalEvents = category.Events?.Count ?? 0;
            var activeEvents = category.Events?.Count(e => e.Status == EventStatus.Published) ?? 0;
            var upcomingEvents = category.Events?.Count(e => e.EventDate > DateTime.UtcNow) ?? 0;
            return new CategoryDetailsViewModel
            {
                Category = category,
                TotalEvents = totalEvents,
                ActiveEvents = activeEvents,
                UpcomingEvents = upcomingEvents,
                RecentEvents = category.Events?.OrderByDescending(e => e.CreatedAt).Take(10).ToList() ?? new List<Event>()
            };
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            try
            {
                var category = await _repo.FindCategoryAsync(id);
                if (category == null) return (false, "Category not found.");
                if (category.Events?.Any() == true)
                    return (false, "Cannot delete category with existing events. Please move or delete the events first.");
                _repo.RemoveCategory(category);
                await _repo.SaveChangesAsync();
                return (true, "Category deleted successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category");
                return (false, "An error occurred while deleting the category. Please try again.");
            }
        }

        public async Task<(bool Success, string Message)> DeleteAjaxAsync(int id)
        {
            return await DeleteAsync(id);
        }

        public async Task<EventCategory?> GetCategoryForDeleteAsync(int id)
        {
            return await _repo.FindCategoryAsync(id);
        }
    }
}


