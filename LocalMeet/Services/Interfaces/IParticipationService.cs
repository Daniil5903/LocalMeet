using LocalMeet.Models;

namespace LocalMeet.Services.Interfaces
{
    public interface IParticipationService
    {
        Task<bool> RegisterAsync(int eventId, string userId);
        Task<bool> UnregisterAsync(int eventId, string userId);
        Task<bool> IsUserRegisteredAsync(int eventId, string userId);
    }
}