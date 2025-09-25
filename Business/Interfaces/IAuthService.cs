using StarTickets.Models;
using StarTickets.Models.ViewModels;
using System.Threading.Tasks;

namespace StarTickets.Services.Interfaces
{
    public interface IAuthService
    {
        Task<bool> RegisterAsync(RegisterViewModel model);
        Task<User> LoginAsync(LoginViewModel model);
        Task<bool> ForgotPasswordAsync(ForgotPasswordViewModel model, string requestScheme);
        Task<User> ValidateResetTokenAsync(string email, string token);
        Task<bool> ResetPasswordAsync(ResetPasswordViewModel model);
        string GeneratePasswordResetToken();
    }
}