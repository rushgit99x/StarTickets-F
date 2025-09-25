using Microsoft.AspNetCore.Mvc;
using StarTickets.Data;
using StarTickets.Models;
using StarTickets.Models.ViewModels;
using StarTickets.Repositories.Interfaces;
using StarTickets.Services;
using StarTickets.Services.Interfaces;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace StarTickets.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IUrlHelper _urlHelper;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IEmailService emailService,
            IUrlHelper urlHelper,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _emailService = emailService;
            _urlHelper = urlHelper;
            _logger = logger;
        }

        public async Task<bool> RegisterAsync(RegisterViewModel model)
        {
            if (await _userRepository.EmailExistsAsync(model.Email))
            {
                return false;
            }

            var user = new User
            {
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                FirstName = model.FirstName,
                LastName = model.LastName,
                Role = model.Role,
                IsActive = true
            };

            try
            {
                await _userRepository.AddUserAsync(user);
                await _emailService.SendWelcomeEmailAsync(user);
                _logger.LogInformation($"User registered successfully: {user.Email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during user registration: {model.Email}");
                return false;
            }
        }

        public async Task<User> LoginAsync(LoginViewModel model)
        {
            var user = await _userRepository.GetUserByEmailAsync(model.Email);
            if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                return user;
            }
            return null;
        }

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordViewModel model, string requestScheme)
        {
            var user = await _userRepository.GetUserByEmailAsync(model.Email);
            if (user == null || !user.IsActive)
            {
                return false;
            }

            var token = GeneratePasswordResetToken();
            var tokenExpiry = DateTime.UtcNow.AddMinutes(10);

            user.ResetToken = token;
            user.ResetTokenExpiry = tokenExpiry;
            user.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _userRepository.UpdateUserAsync(user);
                var resetUrl = _urlHelper.Action("ResetPassword", "Auth", new { token, email = model.Email }, requestScheme);
                await _emailService.SendPasswordResetEmailAsync(user, resetUrl);
                _logger.LogInformation($"Password reset token generated for user: {user.Email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending password reset email: {model.Email}");
                return false;
            }
        }

        public async Task<User> ValidateResetTokenAsync(string email, string token)
        {
            return await _userRepository.GetUserByResetTokenAsync(email, token);
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordViewModel model)
        {
            var user = await _userRepository.GetUserByResetTokenAsync(model.Email, model.Token);
            if (user == null)
            {
                return false;
            }

            try
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                user.ResetToken = null;
                user.ResetTokenExpiry = null;
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateUserAsync(user);
                await _emailService.SendPasswordResetConfirmationEmailAsync(user);
                _logger.LogInformation($"Password reset successfully for user: {user.Email}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resetting password for user: {model.Email}");
                return false;
            }
        }

        public string GeneratePasswordResetToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var tokenBytes = new byte[32];
                rng.GetBytes(tokenBytes);
                return Convert.ToBase64String(tokenBytes)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");
            }
        }
    }
}