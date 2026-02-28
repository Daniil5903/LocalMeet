using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LocalMeet.Models;
using LocalMeet.Services.Interfaces;

namespace LocalMeet.Services
{
    public class AdminService : IAdminService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<List<ApplicationUser>> GetAllUsersAsync()
        {
            return await _userManager.Users.ToListAsync();
        }

        public async Task<List<string>> GetUserRolesAsync(ApplicationUser user)
        {
            return (await _userManager.GetRolesAsync(user)).ToList();
        }

        public async Task LockUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
            await _userManager.UpdateAsync(user);
        }

        public async Task UnlockUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            user.LockoutEnd = null;
            await _userManager.UpdateAsync(user);
        }

        public async Task AddToAdminAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            await _userManager.AddToRoleAsync(user, "Admin");
        }

        public async Task RemoveFromAdminAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return;

            await _userManager.RemoveFromRoleAsync(user, "Admin");
        }

        public async Task<(List<ApplicationUser> Users, int TotalCount)>
            GetPagedUsersAsync(int page, int pageSize)
        {
            var query = _userManager.Users;

            var totalCount = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.Email)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (users, totalCount);
        }
    }
}