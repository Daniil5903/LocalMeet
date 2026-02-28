using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LocalMeet.Services.Interfaces;
using LocalMeet.ViewModels;

namespace LocalMeet.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IStatisticsService _statisticsService;

        private readonly IAdminService _adminService;

        private readonly IEventService _eventService;

        public AdminController(
            IStatisticsService statisticsService,
            IAdminService adminService,
            IEventService eventService)
        {
            _statisticsService = statisticsService;
            _adminService = adminService;
            _eventService = eventService;
        }

        public async Task<IActionResult> Statistics()
        {
            var model = new AdminStatisticsViewModel
            {
                UsersCount = await _statisticsService.GetUsersCountAsync(),
                EventsCount = await _statisticsService.GetEventsCountAsync(),
                ApprovedEvents = await _statisticsService.GetApprovedEventsCountAsync(),
                PendingEvents = await _statisticsService.GetPendingEventsCountAsync(),
                RejectedEvents = await _statisticsService.GetRejectedEventsCountAsync(),
                CancelledEvents = await _statisticsService.GetCancelledEventsCountAsync(),
                ParticipationsCount = await _statisticsService.GetParticipationsCountAsync()
            };

            return View(model);
        }

        public async Task<IActionResult> Users(int page = 1)
        {
            int pageSize = 10;

            var (users, totalCount) =
                await _adminService.GetPagedUsersAsync(page, pageSize);

            var model = new AdminUsersViewModel
            {
                Users = users,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return View(model);
        }

        public async Task<IActionResult> LockUser(string id)
        {
            await _adminService.LockUserAsync(id);
            return RedirectToAction("Users");
        }

        public async Task<IActionResult> UnlockUser(string id)
        {
            await _adminService.UnlockUserAsync(id);
            return RedirectToAction("Users");
        }
        public async Task<IActionResult> PendingEvents()
        {
            var events = await _eventService.GetPendingEventsAsync();
            return View(events);
        }
        public async Task<IActionResult> Approve(int id)
        {
            await _eventService.ApproveEventAsync(id);
            return RedirectToAction("PendingEvents");
        }

        public async Task<IActionResult> Reject(int id)
        {
            await _eventService.RejectEventAsync(id);
            return RedirectToAction("PendingEvents");
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            await _eventService.CancelEventAsync(id, reason);
            return RedirectToAction("PendingEvents");
        }
    }
}