using StarTickets.Models;
using System.Threading.Tasks;

namespace StarTickets.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> EmailExistsAsync(string email);
        Task AddUserAsync(User user);
        Task<User> GetUserByEmailAsync(string email);
        Task<User> GetUserByResetTokenAsync(string email, string token);
        Task UpdateUserAsync(User user);
    }
}