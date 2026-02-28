using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LocalMeet.Models;
using LocalMeet.Services.Interfaces;

namespace LocalMeet.Controllers
{
    [Authorize]
    public class ParticipationController : Controller
    {
        private readonly IParticipationService _participationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ParticipationController(
            IParticipationService participationService,
            UserManager<ApplicationUser> userManager)
        {
            _participationService = participationService;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> Register(int eventId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            await _participationService.RegisterAsync(eventId, user.Id);

            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        [HttpPost]
        public async Task<IActionResult> Unregister(int eventId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            await _participationService.UnregisterAsync(eventId, user.Id);

            return RedirectToAction("Details", "Events", new { id = eventId });
        }
    }
}