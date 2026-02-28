using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LocalMeet.Models;
using LocalMeet.Services.Interfaces;

namespace LocalMeet.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IEventService _eventService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(
            IEventService eventService,
            UserManager<ApplicationUser> userManager)
        {
            _eventService = eventService;
            _userManager = userManager;
        }

        public async Task<IActionResult> MyEvents()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var events = await _eventService.GetUserEventsAsync(user.Id);

            return View(events);
        }
    }
}