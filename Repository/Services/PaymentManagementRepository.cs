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
    // Repository class for handling payment management-related database operations
    public class PaymentManagementRepository : IPaymentManagementRepository
    {
        // Database context for accessing payment-related data
        private readonly ApplicationDbContext _context;

        // Constructor with dependency injection for the database context
        public PaymentManagementRepository(ApplicationDbContext context)
        {
            // Validate context is not null
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Returns a queryable collection of bookings with customer and event details
        public IQueryable<Booking> QueryBookingsWithCustomerAndEvent()
        {
            try
            {
                // Build queryable collection for bookings with related customer and event data
                return _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Event)
                    .AsQueryable();
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while querying bookings with customer and event details.", ex);
            }
        }

        // Retrieves a list of event options (ID and name) ordered by event name
        public async Task<List<(int EventId, string EventName)>> GetEventOptionsAsync()
        {
            try
            {
                // Query database for events, selecting ID and name, ordered by name
                var list = await _context.Events
                    .OrderBy(e => e.EventName)
                    .Select(e => new { e.EventId, e.EventName })
                    .ToListAsync();

                // Convert results to tuple list
                return list.Select(e => (e.EventId, e.EventName)).ToList();
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while retrieving event options.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while retrieving event options.", ex);
            }
        }
    }
}