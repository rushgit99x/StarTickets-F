using Microsoft.EntityFrameworkCore;
using StarTickets.Models;
using StarTickets.Models.ViewModels;
using StarTickets.Repositories.Interfaces;
using StarTickets.Services.Interfaces;

namespace StarTickets.Services
{
    public class BookingManagementService : IBookingManagementService
    {
        private readonly IBookingManagementRepository _repo;
        private readonly ILogger<BookingManagementService> _logger;

        public BookingManagementService(IBookingManagementRepository repo, ILogger<BookingManagementService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<AdminBookingListViewModel> GetIndexAsync(string search, PaymentStatus? paymentStatus, BookingStatus? bookingStatus, int page, int pageSize)
        {
            var query = _repo.QueryBookingsWithDetails();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(b => b.BookingReference.Contains(search) ||
                    (b.Customer != null && (b.Customer.FirstName + " " + b.Customer.LastName).Contains(search)) ||
                    (b.Event != null && b.Event.EventName.Contains(search)));
            if (paymentStatus.HasValue)
                query = query.Where(b => b.PaymentStatus == paymentStatus.Value);
            if (bookingStatus.HasValue)
                query = query.Where(b => b.Status == bookingStatus.Value);

            var total = await query.CountAsync();
            var bookings = await query.OrderByDescending(b => b.BookingDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new AdminBookingListViewModel
            {
                Bookings = bookings,
                Search = search,
                PaymentStatusFilter = paymentStatus,
                BookingStatusFilter = bookingStatus,
                CurrentPage = page,
                PageSize = pageSize,
                TotalBookings = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };
        }

        public async Task<BookingDetailsViewModel?> GetDetailsAsync(int id)
        {
            var booking = await _repo.GetBookingWithFullDetailsAsync(id);
            if (booking == null) return null;
            return new BookingDetailsViewModel
            {
                Booking = booking,
                Event = booking.Event!,
                Venue = booking.Event!.Venue!,
                BookingDetails = booking.BookingDetails?.ToList() ?? new List<BookingDetail>(),
                Tickets = booking.BookingDetails?.SelectMany(d => d.Tickets ?? new List<Ticket>()).ToList() ?? new List<Ticket>(),
                CanCancel = booking.Status == BookingStatus.Active && booking.PaymentStatus != PaymentStatus.Refunded
            };
        }

        public async Task<(bool Success, string Message)> CancelAsync(int id, string? reason)
        {
            try
            {
                var booking = await _repo.GetBookingWithFullDetailsAsync(id);
                if (booking == null) return (false, "Booking not found.");
                if (booking.Status == BookingStatus.Cancelled) return (true, "Booking already cancelled.");
                booking.Status = BookingStatus.Cancelled;
                booking.UpdatedAt = DateTime.UtcNow;
                if (booking.PaymentStatus == PaymentStatus.Completed && booking.BookingDetails != null)
                {
                    foreach (var detail in booking.BookingDetails)
                    {
                        if (detail.TicketCategory != null)
                            detail.TicketCategory.AvailableQuantity += detail.Quantity;
                    }
                }
                await _repo.SaveChangesAsync();
                return (true, "Booking has been cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel booking {BookingId}", id);
                return (false, "Failed to cancel booking.");
            }
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            try
            {
                var booking = await _repo.GetBookingWithFullDetailsAsync(id);
                if (booking == null) return (false, "Booking not found.");
                if (booking.BookingDetails != null)
                {
                    foreach (var detail in booking.BookingDetails)
                    {
                        if (detail.Tickets != null && detail.Tickets.Any())
                            _repo.RemoveTickets(detail.Tickets);
                    }
                    _repo.RemoveBookingDetails(booking.BookingDetails);
                }
                _repo.RemoveBooking(booking);
                await _repo.SaveChangesAsync();
                return (true, "Booking has been deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete booking {BookingId}", id);
                return (false, "Failed to delete booking.");
            }
        }
    }
}


