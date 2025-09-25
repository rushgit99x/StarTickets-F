using StarTickets.Models;

namespace StarTickets.Repositories.Interfaces
{
    public interface IReviewsManagementRepository
    {
        IQueryable<EventRating> QueryRatingsWithRelations();
        Task<EventRating?> FindRatingAsync(int id);
        Task SaveChangesAsync();
        void Remove(EventRating rating);
    }
}


