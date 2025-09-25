using Microsoft.AspNetCore.Mvc.Rendering;
using StarTickets.Models.ViewModels;

namespace StarTickets.Services.Interfaces
{
    public interface IEventOrganizerService
    {
        Task<EventOrganizerDashboardViewModel> GetDashboardAsync(int organizerId);
        Task<ReportsViewModel> GetSalesReportModelAsync(int organizerId, DateTime? startDate, DateTime? endDate, int? eventId);
        Task<List<SelectListItem>> GetOrganizerEventOptionsAsync(int organizerId);

        Task<EventOrganizerProfileViewModel?> GetProfileAsync(int organizerId);
        Task<bool> UpdateProfileAsync(int organizerId, EventOrganizerProfileViewModel model);

        Task<EventOrganizerEventsViewModel> GetMyEventsAsync(int organizerId, string searchTerm, int categoryFilter, StarTickets.Models.EventStatus? statusFilter, int page, int pageSize);
        Task<CreateEventOrganizerViewModel> GetCreateEventFormAsync(int organizerId);
        Task<(bool Success, string Message)> CreateEventAsync(int organizerId, CreateEventOrganizerViewModel model);
        Task<EditEventOrganizerViewModel?> GetEditEventAsync(int organizerId, int eventId);
        Task<(bool Success, string Message)> EditEventAsync(int organizerId, EditEventOrganizerViewModel model);
        Task<EventOrganizerDetailsViewModel?> GetEventDetailsAsync(int organizerId, int eventId);
        Task<(bool Success, string Message)> SubmitForApprovalAsync(int organizerId, int eventId);
        Task<(bool Success, string Message)> DeleteEventAsync(int organizerId, int eventId);
    }
}


