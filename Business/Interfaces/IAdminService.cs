using StarTickets.Models.ViewModels;

namespace StarTickets.Services.Interfaces
{
    public interface IAdminService
    {
        AdminDashboardViewModel BuildDashboard(int adminUserId);
    }
}


