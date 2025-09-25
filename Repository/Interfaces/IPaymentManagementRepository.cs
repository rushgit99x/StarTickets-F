using StarTickets.Models;

namespace StarTickets.Repositories.Interfaces
{
    public interface IPaymentManagementRepository
    {
        IQueryable<Booking> QueryBookingsWithCustomerAndEvent();
        Task<List<(int EventId, string EventName)>> GetEventOptionsAsync();
    }
}


