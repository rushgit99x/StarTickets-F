using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Filters;
using StarTickets.Models;
using StarTickets.Models.ViewModels;

namespace StarTickets.Controllers
{
    [RoleAuthorize("1")] // Admin only
    public class BookingManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BookingManagementController> _logger;

        public BookingManagementController(ApplicationDbContext context, ILogger<BookingManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: BookingManagement
        public async Task<IActionResult> Index(string search = "", PaymentStatus? paymentStatus = null, Models.BookingStatus? bookingStatus = null, int page = 1, int pageSize = 10)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var query = _context.Bookings
                .Include(b => b.Event)!
                    .ThenInclude(e => e!.Venue)
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails)!
                    .ThenInclude(d => d.TicketCategory)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b =>
                    b.BookingReference.Contains(search) ||
                    (b.Customer != null && (b.Customer.FirstName + " " + b.Customer.LastName).Contains(search)) ||
                    (b.Event != null && b.Event.EventName.Contains(search)));
            }

            if (paymentStatus.HasValue)
            {
                query = query.Where(b => b.PaymentStatus == paymentStatus.Value);
            }

            if (bookingStatus.HasValue)
            {
                query = query.Where(b => b.Status == bookingStatus.Value);
            }

            var total = await query.CountAsync();
            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new AdminBookingListViewModel
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

            return View(viewModel);
        }

        // GET: BookingManagement/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var booking = await _context.Bookings
                .Include(b => b.Event)!
                    .ThenInclude(e => e!.Venue)
                .Include(b => b.Event)!
                    .ThenInclude(e => e!.Category)
                .Include(b => b.Customer)
                .Include(b => b.BookingDetails)!
                    .ThenInclude(d => d.TicketCategory)
                .Include(b => b.BookingDetails)!
                    .ThenInclude(d => d.Tickets)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                return NotFound();
            }

            var vm = new BookingDetailsViewModel
            {
                Booking = booking,
                Event = booking.Event!,
                Venue = booking.Event!.Venue!,
                BookingDetails = booking.BookingDetails?.ToList() ?? new List<BookingDetail>(),
                Tickets = booking.BookingDetails?.SelectMany(d => d.Tickets ?? new List<Ticket>()).ToList() ?? new List<Ticket>(),
                CanCancel = booking.Status == Models.BookingStatus.Active && booking.PaymentStatus != PaymentStatus.Refunded
            };

            return View(vm);
        }

        // POST: BookingManagement/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.BookingDetails)!
                        .ThenInclude(d => d.TicketCategory)
                    .FirstOrDefaultAsync(b => b.BookingId == id);

                if (booking == null)
                {
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (booking.Status == Models.BookingStatus.Cancelled)
                {
                    TempData["InfoMessage"] = "Booking already cancelled.";
                    return RedirectToAction("Details", new { id });
                }

                booking.Status = Models.BookingStatus.Cancelled;
                booking.UpdatedAt = DateTime.UtcNow;

                // Optionally restock tickets if payment was completed
                if (booking.PaymentStatus == PaymentStatus.Completed && booking.BookingDetails != null)
                {
                    foreach (var detail in booking.BookingDetails)
                    {
                        if (detail.TicketCategory != null)
                        {
                            detail.TicketCategory.AvailableQuantity += detail.Quantity;
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Booking has been cancelled.";
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel booking {BookingId}", id);
                TempData["ErrorMessage"] = "Failed to cancel booking.";
                return RedirectToAction("Details", new { id });
            }
        }

        // POST: BookingManagement/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            try
            {
                var booking = await _context.Bookings
                    .Include(b => b.BookingDetails)!
                        .ThenInclude(d => d.Tickets)
                    .FirstOrDefaultAsync(b => b.BookingId == id);

                if (booking == null)
                {
                    TempData["ErrorMessage"] = "Booking not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Remove tickets
                if (booking.BookingDetails != null)
                {
                    foreach (var detail in booking.BookingDetails)
                    {
                        if (detail.Tickets != null && detail.Tickets.Any())
                        {
                            _context.Tickets.RemoveRange(detail.Tickets);
                        }
                    }

                    // Remove details
                    _context.BookingDetails.RemoveRange(booking.BookingDetails);
                }

                _context.Bookings.Remove(booking);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Booking has been deleted.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete booking {BookingId}", id);
                TempData["ErrorMessage"] = "Failed to delete booking.";
                return RedirectToAction("Details", new { id });
            }
        }
    }
}


