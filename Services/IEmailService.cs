using StarTickets.Models;

namespace StarTickets.Services
{
    public interface IEmailService
    {
        Task SendWelcomeEmailAsync(User user);
        Task SendEmailAsync(string to, string subject, string body);
        Task SendPasswordResetEmailAsync(User user, string resetUrl);
        Task SendPasswordResetConfirmationEmailAsync(User user);
        Task SendTicketConfirmationEmailAsync(Booking booking);
        Task SendTicketEmailAsync(Ticket ticket, Booking booking);
    }
}