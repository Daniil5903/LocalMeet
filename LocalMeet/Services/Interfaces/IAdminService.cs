using LocalMeet.Models;

namespace LocalMeet.Services.Interfaces
{
    public interface IAdminService
    {
        Task<List<ApplicationUser>> GetAllUsersAsync();
        Task<List<string>> GetUserRolesAsync(ApplicationUser user);
        Task LockUserAsync(string userId);
        Task UnlockUserAsync(string userId);
        Task AddToAdminAsync(string userId);
        Task RemoveFromAdminAsync(string userId);
        Task<(List<ApplicationUser> Users, int TotalCount)>
            GetPagedUsersAsync(int page, int pageSize);
    }
}