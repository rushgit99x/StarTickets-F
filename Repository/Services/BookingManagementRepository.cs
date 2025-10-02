using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StarTickets.Repositories
{
    // Repository class for handling booking management-related database operations
    public class BookingManagementRepository : IBookingManagementRepository
    {
        private readonly ApplicationDbContext _context;

        // Constructor with dependency injection for the database context
        public BookingManagementRepository(ApplicationDbContext context)
        {
            // Validate context is not null
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Returns a queryable collection of bookings with full details
        public IQueryable<Booking> QueryBookingsWithDetails()
        {
            try
            {
                // Build queryable collection for bookings with related event, venue, customer, and ticket details
                return _context.Bookings
                    .Include(b => b.Event)!.ThenInclude(e => e!.Venue)
                    .Include(b => b.Customer)
                    .Include(b => b.BookingDetails)!.ThenInclude(d => d.TicketCategory)
                    .Include(b => b.BookingDetails)!.ThenInclude(d => d.Tickets)
                    .AsQueryable();
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while querying bookings with details.", ex);
            }
        }

        // Retrieves a booking by ID with full details including event, venue, category, customer, and tickets
        public async Task<Booking?> GetBookingWithFullDetailsAsync(int id)
        {
            // Validate booking ID
            if (id <= 0)
            {
                throw new ArgumentException("Booking ID must be greater than zero.", nameof(id));
            }

            try
            {
                // Query database for a booking with full related data
                return await _context.Bookings
                    .Include(b => b.Event)!.ThenInclude(e => e!.Venue)
                    .Include(b => b.Event)!.ThenInclude(e => e!.Category)
                    .Include(b => b.Customer)
                    .Include(b => b.BookingDetails)!.ThenInclude(d => d.TicketCategory)
                    .Include(b => b.BookingDetails)!.ThenInclude(d => d.Tickets)
                    .FirstOrDefaultAsync(b => b.BookingId == id);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while retrieving the booking with full details.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while retrieving the booking with full details.", ex);
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

        // Removes a collection of tickets from the database
        public void RemoveTickets(IEnumerable<Ticket> tickets)
        {
            // Validate tickets input
            if (tickets == null)
            {
                throw new ArgumentNullException(nameof(tickets));
            }

            try
            {
                // Remove tickets from the context
                _context.Tickets.RemoveRange(tickets);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while removing tickets.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while removing tickets.", ex);
            }
        }

        // Removes a collection of booking details from the database
        public void RemoveBookingDetails(IEnumerable<BookingDetail> details)
        {
            // Validate booking details input
            if (details == null)
            {
                throw new ArgumentNullException(nameof(details));
            }

            try
            {
                // Remove booking details from the context
                _context.BookingDetails.RemoveRange(details);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while removing booking details.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while removing booking details.", ex);
            }
        }

        // Removes a booking from the database
        public void RemoveBooking(Booking booking)
        {
            // Validate booking input
            if (booking == null)
            {
                throw new ArgumentNullException(nameof(booking));
            }

            try
            {
                // Remove booking from the context
                _context.Bookings.Remove(booking);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while removing the booking.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while removing the booking.", ex);
            }
        }
    }
}