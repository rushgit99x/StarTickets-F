using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StarTickets.Repositories
{
    // Repository class for handling booking-related database operations
    public class BookingRepository : IBookingRepository
    {
        // Database context for accessing booking-related data
        private readonly ApplicationDbContext _context;

        // Constructor with dependency injection for the database context
        public BookingRepository(ApplicationDbContext context)
        {
            // Validate context is not null
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Retrieves a published event with its category, venue, and active ticket categories
        public async Task<Event?> GetPublishedEventForBookingAsync(int eventId)
        {
            // Validate event ID
            if (eventId <= 0)
            {
                throw new ArgumentException("Event ID must be greater than zero.", nameof(eventId));
            }

            try
            {
                // Query database for a published, active event with related data
                return await _context.Events
                    .Include(e => e.Category)
                    .Include(e => e.Venue)
                    .Include(e => e.TicketCategories.Where(tc => tc.IsActive))
                    .FirstOrDefaultAsync(e => e.EventId == eventId && e.IsActive && e.Status == EventStatus.Published);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while retrieving the published event.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while retrieving the published event.", ex);
            }
        }

        // Retrieves an event with its category, venue, and active ticket categories for reloading
        public async Task<Event?> GetEventForReloadAsync(int eventId)
        {
            // Validate event ID
            if (eventId <= 0)
            {
                throw new ArgumentException("Event ID must be greater than zero.", nameof(eventId));
            }

            try
            {
                // Query database for an event with related data
                return await _context.Events
                    .Include(e => e.Category)
                    .Include(e => e.Venue)
                    .Include(e => e.TicketCategories.Where(tc => tc.IsActive))
                    .FirstOrDefaultAsync(e => e.EventId == eventId);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while retrieving the event for reload.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while retrieving the event for reload.", ex);
            }
        }

        // Retrieves a user by their ID
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            // Validate user ID
            if (userId <= 0)
            {
                throw new ArgumentException("User ID must be greater than zero.", nameof(userId));
            }

            try
            {
                // Query database for a user by ID
                return await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while retrieving the user by ID.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while retrieving the user by ID.", ex);
            }
        }

        // Adds a new booking to the database
        public async Task AddBookingAsync(Booking booking)
        {
            // Validate booking input
            if (booking == null)
            {
                throw new ArgumentNullException(nameof(booking));
            }

            try
            {
                // Add booking to the context and save to database
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while adding the booking to the database.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while adding the booking.", ex);
            }
        }

        // Adds a collection of tickets to the database
        public void AddTickets(IEnumerable<Ticket> tickets)
        {
            // Validate tickets input
            if (tickets == null)
            {
                throw new ArgumentNullException(nameof(tickets));
            }

            try
            {
                // Add tickets to the context
                _context.Tickets.AddRange(tickets);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while adding tickets to the database.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while adding tickets.", ex);
            }
        }

        // Saves changes to the database
        public async Task SaveChangesAsync()
        {
            try
            {
                // Persist changes to the database
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Handle concurrency conflicts
                throw new InvalidOperationException("A concurrency error occurred while saving changes.", ex);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while saving changes to the database.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while saving changes.", ex);
            }
        }

        // Begins a database transaction
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            try
            {
                // Start a new database transaction
                return await _context.Database.BeginTransactionAsync();
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An error occurred while starting a database transaction.", ex);
            }
        }

        // Retrieves a completed booking with related event, venue, customer, and ticket details
        public async Task<Booking?> GetCompletedBookingForEmailAsync(int bookingId, int customerId)
        {
            // Validate input parameters
            if (bookingId <= 0)
            {
                throw new ArgumentException("Booking ID must be greater than zero.", nameof(bookingId));
            }
            if (customerId <= 0)
            {
                throw new ArgumentException("Customer ID must be greater than zero.", nameof(customerId));
            }

            try
            {
                // Query database for a completed booking with related data
                return await _context.Bookings
                    .Include(b => b.Event)
                        .ThenInclude(e => e.Venue)
                    .Include(b => b.Event)
                        .ThenInclude(e => e.Category)
                    .Include(b => b.Customer)
                    .Include(b => b.BookingDetails)
                        .ThenInclude(bd => bd.TicketCategory)
                    .Include(b => b.BookingDetails)
                        .ThenInclude(bd => bd.Tickets)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId &&
                                             b.CustomerId == customerId &&
                                             b.PaymentStatus == PaymentStatus.Completed);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while retrieving the completed booking.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while retrieving the completed booking.", ex);
            }
        }

        // Retrieves a pending booking with related event, venue, customer, and ticket details
        public async Task<Booking?> GetPendingBookingForPaymentAsync(int bookingId, int customerId)
        {
            // Validate input parameters
            if (bookingId <= 0)
            {
                throw new ArgumentException("Booking ID must be greater than zero.", nameof(bookingId));
            }
            if (customerId <= 0)
            {
                throw new ArgumentException("Customer ID must be greater than zero.", nameof(customerId));
            }

            try
            {
                // Query database for a pending booking with related data
                return await _context.Bookings
                    .Include(b => b.Event)
                        .ThenInclude(e => e.Venue)
                    .Include(b => b.Event)
                        .ThenInclude(e => e.Category)
                    .Include(b => b.Customer)
                    .Include(b => b.BookingDetails)
                        .ThenInclude(bd => bd.TicketCategory)
                    .Include(b => b.BookingDetails)
                        .ThenInclude(bd => bd.Tickets)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId &&
                                             b.CustomerId == customerId &&
                                             b.PaymentStatus == PaymentStatus.Pending);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while retrieving the pending booking.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while retrieving the pending booking.", ex);
            }
        }

        // Retrieves a booking with its event, venue, and ticket details
        public async Task<Booking?> GetBookingWithTicketsAsync(int bookingId, int customerId)
        {
            // Validate input parameters
            if (bookingId <= 0)
            {
                throw new ArgumentException("Booking ID must be greater than zero.", nameof(bookingId));
            }
            if (customerId <= 0)
            {
                throw new ArgumentException("Customer ID must be greater than zero.", nameof(customerId));
            }

            try
            {
                // Query database for a booking with related data
                return await _context.Bookings
                    .Include(b => b.Event)
                        .ThenInclude(e => e.Venue)
                    .Include(b => b.BookingDetails)
                        .ThenInclude(d => d.TicketCategory)
                    .Include(b => b.BookingDetails)
                        .ThenInclude(d => d.Tickets)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == customerId);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while retrieving the booking with tickets.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while retrieving the booking with tickets.", ex);
            }
        }

        // Retrieves an active promotional campaign by its discount code
        public async Task<PromotionalCampaign?> GetActivePromoByCodeAsync(string code)
        {
            // Validate code input
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Discount code cannot be null or empty.", nameof(code));
            }

            try
            {
                // Query database for an active promotional campaign with valid date and usage limits
                return await _context.PromotionalCampaigns
                    .FirstOrDefaultAsync(p => p.DiscountCode == code &&
                                             p.IsActive &&
                                             DateTime.UtcNow >= p.StartDate &&
                                             DateTime.UtcNow <= p.EndDate &&
                                             (p.MaxUsage == null || p.CurrentUsage < p.MaxUsage));
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while retrieving the promotional campaign by code.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while retrieving the promotional campaign by code.", ex);
            }
        }

        // Retrieves a promotional campaign by its ID
        public async Task<PromotionalCampaign?> GetPromoByIdAsync(int promoId)
        {
            // Validate promo ID
            if (promoId <= 0)
            {
                throw new ArgumentException("Promo ID must be greater than zero.", nameof(promoId));
            }

            try
            {
                // Query database for a promotional campaign by ID
                return await _context.PromotionalCampaigns.FindAsync(promoId);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while retrieving the promotional campaign by ID.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while retrieving the promotional campaign by ID.", ex);
            }
        }

        // Retrieves a ticket by its number and associated user ID
        public async Task<Ticket?> GetTicketByNumberForUserAsync(string ticketNumber, int userId)
        {
            // Validate input parameters
            if (string.IsNullOrWhiteSpace(ticketNumber))
            {
                throw new ArgumentException("Ticket number cannot be null or empty.", nameof(ticketNumber));
            }
            if (userId <= 0)
            {
                throw new ArgumentException("User ID must be greater than zero.", nameof(userId));
            }

            try
            {
                // Query database for a ticket with related booking and event data
                return await _context.Tickets
                    .Include(t => t.BookingDetail)
                        .ThenInclude(bd => bd.Booking)
                            .ThenInclude(b => b.Event)
                    .Include(t => t.BookingDetail.TicketCategory)
                    .FirstOrDefaultAsync(t => t.TicketNumber == ticketNumber && t.BookingDetail.Booking.CustomerId == userId);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while retrieving the ticket by number.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while retrieving the ticket by number.", ex);
            }
        }
    }
}