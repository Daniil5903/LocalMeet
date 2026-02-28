namespace LocalMeet.Services.Interfaces
{
    public interface IStatisticsService
    {
        Task<int> GetUsersCountAsync();
        Task<int> GetEventsCountAsync();
        Task<int> GetApprovedEventsCountAsync();
        Task<int> GetPendingEventsCountAsync();
        Task<int> GetRejectedEventsCountAsync();
        Task<int> GetCancelledEventsCountAsync();
        Task<int> GetParticipationsCountAsync();
    }
}