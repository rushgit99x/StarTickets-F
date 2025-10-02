using Microsoft.EntityFrameworkCore;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Repositories.Interfaces;
using System;
using System.Threading.Tasks;

namespace StarTickets.Repositories
{
    // Repository class for handling user-related database operations
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        // Constructor with dependency injection for the database context
        public UserRepository(ApplicationDbContext context)
        {
            // Validate context is not null
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Checks if a given email already exists in the database
        public async Task<bool> EmailExistsAsync(string email)
        {
            // Validate email input
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be null or empty.", nameof(email));
            }

            try
            {
                // Query database for email existence
                return await _context.Users.AnyAsync(u => u.Email == email);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while checking email existence.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while checking email existence.", ex);
            }
        }

        // Adds a new user to the database
        public async Task AddUserAsync(User user)
        {
            // Validate user input
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            try
            {
                // Add user to the context and save to database
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while adding the user to the database.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while adding the user.", ex);
            }
        }

        // Retrieves a user by their email address
        public async Task<User> GetUserByEmailAsync(string email)
        {
            // Validate email input
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be null or empty.", nameof(email));
            }

            try
            {
                // Query database for user with matching email
                return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while retrieving the user by email.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while retrieving the user by email.", ex);
            }
        }

        // Retrieves a user by email and valid reset token
        public async Task<User> GetUserByResetTokenAsync(string email, string token)
        {
            // Validate email input
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be null or empty.", nameof(email));
            }

            // Validate token input
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Token cannot be null or empty.", nameof(token));
            }

            try
            {
                // Query database for user with matching email, token, and valid token expiry
                return await _context.Users.FirstOrDefaultAsync(u =>
                    u.Email == email &&
                    u.ResetToken == token &&
                    u.ResetTokenExpiry > DateTime.UtcNow);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while retrieving the user by reset token.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while retrieving the user by reset token.", ex);
            }
        }

        // Updates an existing user in the database
        public async Task UpdateUserAsync(User user)
        {
            // Validate user input
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            try
            {
                // Update user in the context and save to database
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Handle concurrency conflicts
                throw new InvalidOperationException("A concurrency error occurred while updating the user.", ex);
            }
            catch (DbUpdateException ex)
            {
                // Handle database-specific errors
                throw new InvalidOperationException("An error occurred while updating the user in the database.", ex);
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                throw new InvalidOperationException("An unexpected error occurred while updating the user.", ex);
            }
        }
    }
}