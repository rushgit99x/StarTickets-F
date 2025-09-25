using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Repositories.Interfaces;

namespace StarTickets.Repositories
{
    public class CategoryManagementRepository : ICategoryManagementRepository
    {
        private readonly ApplicationDbContext _context;

        public CategoryManagementRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<EventCategory> QueryCategoriesWithEvents()
        {
            return _context.EventCategories.Include(c => c.Events).AsQueryable();
        }

        public Task<EventCategory?> FindCategoryAsync(int id)
        {
            return _context.EventCategories.Include(c => c.Events).FirstOrDefaultAsync(c => c.CategoryId == id);
        }

        public async Task<bool> CategoryNameExistsAsync(string name, int? excludeId = null)
        {
            var q = _context.EventCategories.AsQueryable();
            if (excludeId.HasValue)
                q = q.Where(c => c.CategoryId != excludeId.Value);
            return await q.AnyAsync(c => c.CategoryName.ToLower() == name.ToLower());
        }

        public async Task AddCategoryAsync(EventCategory category)
        {
            _context.EventCategories.Add(category);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public void RemoveCategory(EventCategory category)
        {
            _context.EventCategories.Remove(category);
        }
    }
}


