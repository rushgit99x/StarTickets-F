using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Repositories.Interfaces;

namespace StarTickets.Repositories
{
    public class ReviewsManagementRepository : IReviewsManagementRepository
    {
        private readonly ApplicationDbContext _context;

        public ReviewsManagementRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<EventRating> QueryRatingsWithRelations()
        {
            return _context.EventRatings.Include(r => r.Event).Include(r => r.Customer).AsQueryable();
        }

        public Task<EventRating?> FindRatingAsync(int id)
        {
            return _context.EventRatings.FindAsync(id).AsTask();
        }

        public Task SaveChangesAsync() => _context.SaveChangesAsync();

        public void Remove(EventRating rating) => _context.EventRatings.Remove(rating);
    }
}


