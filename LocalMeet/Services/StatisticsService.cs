using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LocalMeet.Data;
using LocalMeet.Models;
using LocalMeet.Models.Enums;
using LocalMeet.Services.Interfaces;

namespace LocalMeet.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StatisticsService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<int> GetUsersCountAsync()
        {
            return await _userManager.Users.CountAsync();
        }

        public async Task<int> GetEventsCountAsync()
        {
            return await _context.Events.CountAsync();
        }

        public async Task<int> GetApprovedEventsCountAsync()
        {
            return await _context.Events
                .CountAsync(e => e.Status == EventStatus.Approved);
        }

        public async Task<int> GetPendingEventsCountAsync()
        {
            return await _context.Events
                .CountAsync(e => e.Status == EventStatus.Pending);
        }

        public async Task<int> GetRejectedEventsCountAsync()
        {
            return await _context.Events
                .CountAsync(e => e.Status == EventStatus.Rejected);
        }

        public async Task<int> GetCancelledEventsCountAsync()
        {
            return await _context.Events
                .CountAsync(e => e.Status == EventStatus.Cancelled);
        }

        public async Task<int> GetParticipationsCountAsync()
        {
            return await _context.Participations.CountAsync();
        }
    }
}