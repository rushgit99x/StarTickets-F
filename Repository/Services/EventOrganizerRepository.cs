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
    // Repository class for handling event organizer-related database operations
    public class EventOrganizerRepository : IEventOrganizerRepository
    {
        // Database context for accessing event organizer-related data
        private readonly ApplicationDbContext _context;

        // Constructor with dependency injection for the database context
        public EventOrganizerRepository(ApplicationDbContext context)
        {
            // Validate context is not null
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Retrieves a user by their ID without tracking
        public async Task<User?> GetUserByIdAsync(int userId)
        {
            // Validate user ID
            if (userId <= 0)
            {
                throw new ArgumentException("User ID must be greater than zero.", nameof(userId));
            }

            try
            {
                // Query database for a user by ID without tracking
                return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
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

        // Retrieves all events for a specific organizer with related ticket categories and bookings
        public async Task<List<Event>> GetOrganizerEventsAsync(int organizerId)
        {
            // Validate organizer ID
            if (organizerId <= 0)
            {
                throw new ArgumentException("Organizer ID must be greater than zero.", nameof(organizerId));
            }

            try
            {
                // Query database for events with related ticket categories and bookings
                return await _context.Events
                    .Where(e => e.OrganizerId == organizerId)
                    .Include(e => e.TicketCategories)
                    .Include(e => e.Bookings)
                        .ThenInclude(b => b.BookingDetails)
                    .ToListAsync();
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while retrieving organizer events.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while retrieving organizer events.", ex);
            }
        }

        // Returns a queryable collection of organizer events with related data
        public IQueryable<Event> QueryOrganizerEvents(int organizerId)
        {
            // Validate organizer ID
            if (organizerId <= 0)
            {
                throw new ArgumentException("Organizer ID must be greater than zero.", nameof(organizerId));
            }

            try
            {
                // Build queryable collection for events with related data
                return _context.Events
                    .Where(e => e.OrganizerId == organizerId)
                    .Include(e => e.Category)
                    .Include(e => e.Venue)
                    .Include(e => e.TicketCategories)
                    .Include(e => e.Bookings)
                        .ThenInclude(b => b.BookingDetails)
                    .AsQueryable();
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while querying organizer events.", ex);
            }
        }

        // Retrieves all event categories
        public async Task<List<EventCategory>> GetCategoriesAsync()
        {
            try
            {
                // Query database for non-null event categories
                return await _context.EventCategories
                    .Where(c => c.CategoryName != null)
                    .ToListAsync();
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while retrieving event categories.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while retrieving event categories.", ex);
            }
        }

        // Retrieves all active venues
        public async Task<List<Venue>> GetActiveVenuesAsync()
        {
            try
            {
                // Query database for active venues
                return await _context.Venues
                    .Where(v => v.IsActive)
                    .ToListAsync();
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while retrieving active venues.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while retrieving active venues.", ex);
            }
        }

        // Adds a new event to the database
        public async Task AddEventAsync(Event ev)
        {
            // Validate event input
            if (ev == null)
            {
                throw new ArgumentNullException(nameof(ev));
            }

            try
            {
                // Add event to the context and save to database
                _context.Events.Add(ev);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while adding the event to the database.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while adding the event.", ex);
            }
        }

        // Adds ticket categories to the database
        public async Task AddTicketCategoriesAsync(IEnumerable<TicketCategory> categories)
        {
            // Validate categories input
            if (categories == null)
            {
                throw new ArgumentNullException(nameof(categories));
            }

            try
            {
                // Add ticket categories to the context and save to database
                _context.TicketCategories.AddRange(categories);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while adding ticket categories to the database.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while adding ticket categories.", ex);
            }
        }

        // Retrieves an event for a specific organizer with optional related data
        public async Task<Event?> GetEventForOrganizerAsync(int organizerId, int eventId, bool includeTickets = false, bool includeBookings = false, bool includeCategoryVenue = false)
        {
            // Validate input parameters
            if (organizerId <= 0)
            {
                throw new ArgumentException("Organizer ID must be greater than zero.", nameof(organizerId));
            }
            if (eventId <= 0)
            {
                throw new ArgumentException("Event ID must be greater than zero.", nameof(eventId));
            }

            try
            {
                // Build query with optional includes based on parameters
                var q = _context.Events.AsQueryable();
                if (includeTickets)
                    q = q.Include(e => e.TicketCategories);
                if (includeBookings)
                    q = q.Include(e => e.Bookings).ThenInclude(b => b.BookingDetails);
                if (includeCategoryVenue)
                    q = q.Include(e => e.Category).Include(e => e.Venue);

                // Query database for the event
                return await q.FirstOrDefaultAsync(e => e.EventId == eventId && e.OrganizerId == organizerId);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while retrieving the event for the organizer.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while retrieving the event for the organizer.", ex);
            }
        }

        // Checks if an event has any associated bookings
        public async Task<bool> HasBookingsAsync(int eventId)
        {
            // Validate event ID
            if (eventId <= 0)
            {
                throw new ArgumentException("Event ID must be greater than zero.", nameof(eventId));
            }

            try
            {
                // Query database for existence of bookings
                return await _context.Bookings.AnyAsync(b => b.EventId == eventId);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while checking for event bookings.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while checking for event bookings.", ex);
            }
        }

        // Removes ticket categories from the database
        public void RemoveTicketCategories(IEnumerable<TicketCategory> ticketCategories)
        {
            // Validate ticket categories input
            if (ticketCategories == null)
            {
                throw new ArgumentNullException(nameof(ticketCategories));
            }

            try
            {
                // Remove ticket categories from the context
                _context.TicketCategories.RemoveRange(ticketCategories);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while removing ticket categories.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while removing ticket categories.", ex);
            }
        }

        // Removes an event from the database
        public void RemoveEvent(Event ev)
        {
            // Validate event input
            if (ev == null)
            {
                throw new ArgumentNullException(nameof(ev));
            }

            try
            {
                // Remove event from the context
                _context.Events.Remove(ev);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while removing the event.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while removing the event.", ex);
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

        // Returns a queryable collection of completed bookings within a date range for an organizer
        public IQueryable<Booking> QueryBookingsInRangeForOrganizer(int organizerId, DateTime start, DateTime end)
        {
            // Validate input parameters
            if (organizerId <= 0)
            {
                throw new ArgumentException("Organizer ID must be greater than zero.", nameof(organizerId));
            }
            if (start > end)
            {
                throw new ArgumentException("Start date must be earlier than or equal to end date.", nameof(start));
            }

            try
            {
                // Build queryable collection for completed bookings within date range
                return _context.Bookings
                    .AsNoTracking()
                    .Include(b => b.Event)
                    .Where(b => b.BookingDate >= start &&
                               b.BookingDate <= end &&
                               b.PaymentStatus == PaymentStatus.Completed &&
                               b.Event!.OrganizerId == organizerId);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while querying bookings in range.", ex);
            }
        }
    }
}