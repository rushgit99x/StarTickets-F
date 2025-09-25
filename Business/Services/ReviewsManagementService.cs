using Microsoft.EntityFrameworkCore;
using StarTickets.Models.ViewModels;
using StarTickets.Repositories.Interfaces;
using StarTickets.Services.Interfaces;

namespace StarTickets.Services
{
    public class ReviewsManagementService : IReviewsManagementService
    {
        private readonly IReviewsManagementRepository _repo;
        private readonly ILogger<ReviewsManagementService> _logger;

        public ReviewsManagementService(IReviewsManagementRepository repo, ILogger<ReviewsManagementService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<ReviewsManagementListViewModel> GetIndexAsync(string search, int? rating, bool? approved, int page, int pageSize)
        {
            var query = _repo.QueryRatingsWithRelations();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r => (r.Event != null && r.Event.EventName.Contains(search)) || (r.Customer != null && ((r.Customer.FirstName + " " + r.Customer.LastName).Contains(search) || r.Customer.Email.Contains(search))) || (r.Review != null && r.Review.Contains(search)));
            }
            if (rating.HasValue) query = query.Where(r => r.Rating == rating.Value);
            if (approved.HasValue) query = query.Where(r => r.IsApproved == approved.Value);
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return new ReviewsManagementListViewModel
            {
                Ratings = items,
                Search = search,
                RatingFilter = rating,
                ApprovedFilter = approved,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };
        }

        public async Task<(bool Success, string Message)> ApproveAsync(int id)
        {
            var rating = await _repo.FindRatingAsync(id);
            if (rating == null) return (false, "Review not found.");
            rating.IsApproved = true;
            await _repo.SaveChangesAsync();
            return (true, "Review approved.");
        }

        public async Task<(bool Success, string Message)> RejectAsync(int id)
        {
            var rating = await _repo.FindRatingAsync(id);
            if (rating == null) return (false, "Review not found.");
            rating.IsApproved = false;
            await _repo.SaveChangesAsync();
            return (true, "Review rejected.");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            try
            {
                var rating = await _repo.FindRatingAsync(id);
                if (rating == null) return (false, "Review not found.");
                _repo.Remove(rating);
                await _repo.SaveChangesAsync();
                return (true, "Review deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete review {RatingId}", id);
                return (false, "Failed to delete review.");
            }
        }
    }
}


