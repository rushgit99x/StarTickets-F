using Microsoft.EntityFrameworkCore;
using StarTickets.Models;
using StarTickets.Models.ViewModels;
using StarTickets.Repositories.Interfaces;
using StarTickets.Services.Interfaces;

namespace StarTickets.Services
{
    public class EventManagementService : IEventManagementService
    {
        private readonly IEventManagementRepository _repository;
        private readonly ILogger<EventManagementService> _logger;

        public EventManagementService(IEventManagementRepository repository, ILogger<EventManagementService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<EventManagementViewModel> GetIndexAsync(string searchTerm, int categoryFilter, EventStatus? statusFilter, int page, int pageSize)
        {
            var query = _repository.QueryEvents();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(e => e.EventName.Contains(searchTerm) ||
                                         e.Description!.Contains(searchTerm) ||
                                         e.Venue!.VenueName.Contains(searchTerm));
            }

            if (categoryFilter > 0)
            {
                query = query.Where(e => e.CategoryId == categoryFilter);
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(e => e.Status == statusFilter.Value);
            }

            var totalEvents = await query.CountAsync();
            var events = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var categories = await _repository.GetAllCategoriesAsync();

            return new EventManagementViewModel
            {
                Events = events,
                Categories = categories,
                SearchTerm = searchTerm,
                CategoryFilter = categoryFilter,
                StatusFilter = statusFilter,
                CurrentPage = page,
                PageSize = pageSize,
                TotalEvents = totalEvents,
                TotalPages = (int)Math.Ceiling((double)totalEvents / pageSize)
            };
        }

        public async Task<CreateEventViewModel> GetCreateFormAsync()
        {
            var categories = await _repository.GetAllCategoriesAsync();
            var venues = await _repository.GetActiveVenuesAsync();
            var organizers = await _repository.GetActiveOrganizersAsync();

            return new CreateEventViewModel
            {
                Categories = categories,
                Venues = venues,
                Organizers = organizers,
                EventDate = DateTime.Now.AddDays(7),
                EndDate = DateTime.Now.AddDays(7).AddHours(3)
            };
        }

        public async Task<bool> CreateAsync(CreateEventViewModel model)
        {
            var eventEntity = new Event
            {
                EventName = model.EventName,
                Description = model.Description,
                EventDate = model.EventDate,
                EndDate = model.EndDate,
                VenueId = model.VenueId,
                OrganizerId = model.OrganizerId,
                CategoryId = model.CategoryId,
                BandName = model.BandName,
                Performer = model.Performer,
                ImageUrl = model.ImageUrl,
                Status = EventStatus.Published,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                await _repository.AddEventAsync(eventEntity);

                if (model.TicketCategories != null && model.TicketCategories.Any())
                {
                    var ticketCategories = model.TicketCategories
                        .Where(tc => !string.IsNullOrWhiteSpace(tc.CategoryName))
                        .Select(tc => new TicketCategory
                        {
                            EventId = eventEntity.EventId,
                            CategoryName = tc.CategoryName,
                            Price = tc.Price,
                            TotalQuantity = tc.TotalQuantity,
                            AvailableQuantity = tc.TotalQuantity,
                            Description = tc.Description,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        });

                    await _repository.AddTicketCategoriesAsync(ticketCategories);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                return false;
            }
        }

        public async Task<EditEventViewModel?> GetEditFormAsync(int id)
        {
            var eventEntity = await _repository.GetEventWithCategoriesAsync(id);
            if (eventEntity == null)
            {
                return null;
            }

            var categories = await _repository.GetAllCategoriesAsync();
            var venues = await _repository.GetActiveVenuesAsync();
            var organizers = await _repository.GetActiveOrganizersAsync();

            return new EditEventViewModel
            {
                EventId = eventEntity.EventId,
                EventName = eventEntity.EventName,
                Description = eventEntity.Description,
                EventDate = eventEntity.EventDate,
                EndDate = eventEntity.EndDate,
                VenueId = eventEntity.VenueId,
                OrganizerId = eventEntity.OrganizerId,
                CategoryId = eventEntity.CategoryId,
                BandName = eventEntity.BandName,
                Performer = eventEntity.Performer,
                ImageUrl = eventEntity.ImageUrl,
                Status = eventEntity.Status,
                IsActive = eventEntity.IsActive,
                Categories = categories,
                Venues = venues,
                Organizers = organizers,
                TicketCategories = eventEntity.TicketCategories?.Select(tc => new TicketCategoryViewModel
                {
                    TicketCategoryId = tc.TicketCategoryId,
                    CategoryName = tc.CategoryName,
                    Price = tc.Price,
                    TotalQuantity = tc.TotalQuantity,
                    AvailableQuantity = tc.AvailableQuantity,
                    Description = tc.Description,
                    IsActive = tc.IsActive
                }).ToList() ?? new List<TicketCategoryViewModel>()
            };
        }

        public async Task<bool> EditAsync(EditEventViewModel model)
        {
            var eventEntity = await _repository.GetEventWithCategoriesAsync(model.EventId);
            if (eventEntity == null)
            {
                return false;
            }

            try
            {
                eventEntity.EventName = model.EventName;
                eventEntity.Description = model.Description;
                eventEntity.EventDate = model.EventDate;
                eventEntity.EndDate = model.EndDate;
                eventEntity.VenueId = model.VenueId;
                eventEntity.OrganizerId = model.OrganizerId;
                eventEntity.CategoryId = model.CategoryId;
                eventEntity.BandName = model.BandName;
                eventEntity.Performer = model.Performer;
                eventEntity.ImageUrl = model.ImageUrl;
                eventEntity.Status = model.Status;
                eventEntity.IsActive = model.IsActive;
                eventEntity.UpdatedAt = DateTime.UtcNow;

                if (model.TicketCategories != null)
                {
                    var existingCategories = eventEntity.TicketCategories?.ToList() ?? new List<TicketCategory>();
                    _repository.RemoveTicketCategories(existingCategories);

                    var newCategories = model.TicketCategories
                        .Where(tc => !string.IsNullOrWhiteSpace(tc.CategoryName))
                        .Select(tc => new TicketCategory
                        {
                            EventId = eventEntity.EventId,
                            CategoryName = tc.CategoryName,
                            Price = tc.Price,
                            TotalQuantity = tc.TotalQuantity,
                            AvailableQuantity = tc.AvailableQuantity,
                            Description = tc.Description,
                            IsActive = tc.IsActive,
                            CreatedAt = DateTime.UtcNow
                        });
                    await _repository.AddTicketCategoriesAsync(newCategories);
                }

                await _repository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event");
                return false;
            }
        }

        public async Task<EventDetailsViewModel?> GetDetailsAsync(int id)
        {
            var eventEntity = await _repository.GetEventWithDetailsAsync(id);
            if (eventEntity == null)
            {
                return null;
            }

            var totalTicketsSold = eventEntity.Bookings?
                .Where(b => b.PaymentStatus == PaymentStatus.Completed)
                .SelectMany(b => b.BookingDetails!)
                .Sum(bd => bd.Quantity) ?? 0;

            var totalRevenue = eventEntity.Bookings?
                .Where(b => b.PaymentStatus == PaymentStatus.Completed)
                .Sum(b => b.FinalAmount) ?? 0;

            var totalCapacity = eventEntity.TicketCategories?.Sum(tc => tc.TotalQuantity) ?? 0;

            return new EventDetailsViewModel
            {
                Event = eventEntity,
                TotalTicketsSold = totalTicketsSold,
                TotalRevenue = totalRevenue,
                TotalCapacity = totalCapacity,
                TicketsSoldPercentage = totalCapacity > 0 ? (double)totalTicketsSold / totalCapacity * 100 : 0
            };
        }

        public async Task<(bool Success, string Message)> UpdateStatusAsync(int id, EventStatus status)
        {
            try
            {
                var eventEntity = await _repository.GetEventWithCategoriesAsync(id);
                if (eventEntity == null)
                {
                    return (false, "Event not found.");
                }

                eventEntity.Status = status;
                eventEntity.UpdatedAt = DateTime.UtcNow;
                await _repository.SaveChangesAsync();
                return (true, $"Event status updated to {status}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event status");
                return (false, "An error occurred while updating the event status.");
            }
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            try
            {
                var eventEntity = await _repository.GetEventForDeletionAsync(id);
                if (eventEntity == null)
                {
                    return (false, "Event not found.");
                }

                if (eventEntity.Bookings?.Any() == true)
                {
                    return (false, "Cannot delete event with existing bookings. Consider changing the status to 'Cancelled' instead.");
                }

                if (eventEntity.TicketCategories?.Any() == true)
                {
                    _repository.RemoveTicketCategories(eventEntity.TicketCategories);
                }

                _repository.RemoveEvent(eventEntity);
                await _repository.SaveChangesAsync();
                return (true, "Event deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event with ID {EventId}", id);
                return (false, "An error occurred while deleting the event. Please try again.");
            }
        }
    }
}


