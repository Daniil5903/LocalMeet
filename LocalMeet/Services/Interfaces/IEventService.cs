using LocalMeet.Models;
using LocalMeet.Models.Enums;

namespace LocalMeet.Services.Interfaces
{
    public interface IEventService
    {
        Task CreateEventAsync(Event ev);
        Task<List<Event>> GetApprovedEventsAsync();
        Task<Event?> GetByIdAsync(int id);
        Task<List<Event>> GetUserEventsAsync(string userId);
        Task<List<Event>> GetPendingEventsAsync();
        Task ApproveEventAsync(int eventId);
        Task RejectEventAsync(int eventId);
        Task CancelEventAsync(int eventId, string reason);
        Task<(IEnumerable<Event> Events, int TotalCount)> GetFilteredEventsAsync(
            int? categoryId,
            string? search,
            bool showPast,
            int page,
            int pageSize);
    }
}