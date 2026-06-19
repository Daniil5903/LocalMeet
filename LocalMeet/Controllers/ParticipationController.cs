using LocalMeet.Data;
using LocalMeet.Models.Entities;
using LocalMeet.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalMeet.Controllers
{
    [Authorize]
    public class ParticipationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ParticipationController(
            ApplicationDbContext context,
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(int eventId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (currentUser.IsBlocked)
            {
                TempData["ErrorMessage"] = "Заблокированный пользователь не может участвовать в мероприятиях";
                return RedirectToAction("Details", "Events", new { id = eventId });
            }

            var eventEntity = await _context.Events
                .Include(e => e.Participations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventEntity == null)
            {
                return NotFound();
            }

            if (eventEntity.Status != EventStatus.Approved)
            {
                TempData["ErrorMessage"] = "Регистрация доступна только на одобренные мероприятия";
                return RedirectToAction("Details", "Events", new { id = eventId });
            }

            if (eventEntity.EventDate <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Нельзя зарегистрироваться на прошедшее мероприятие";
                return RedirectToAction("Details", "Events", new { id = eventId });
            }

            if (eventEntity.CreatorId == currentUser.Id)
            {
                TempData["ErrorMessage"] = "Организатор не может зарегистрироваться на собственное мероприятие";
                return RedirectToAction("Details", "Events", new { id = eventId });
            }

            var alreadyJoined = await _context.Participations
                .AnyAsync(p => p.EventId == eventId && p.UserId == currentUser.Id);

            if (alreadyJoined)
            {
                TempData["ErrorMessage"] = "Вы уже зарегистрированы на это мероприятие";
                return RedirectToAction("Details", "Events", new { id = eventId });
            }

            var participantsCount = await _context.Participations
                .CountAsync(p => p.EventId == eventId);

            if (participantsCount >= eventEntity.MaxParticipants)
            {
                TempData["ErrorMessage"] = "На мероприятии больше нет свободных мест";
                return RedirectToAction("Details", "Events", new { id = eventId });
            }

            var participation = new Participation
            {
                UserId = currentUser.Id,
                EventId = eventEntity.Id,
                CreatedAt = DateTime.Now
            };

            _context.Participations.Add(participation);

            _context.Notifications.Add(new Notification
            {
                UserId = eventEntity.CreatorId,
                Title = "Новый участник мероприятия",
                Message = $"{currentUser.FirstName} {currentUser.LastName} зарегистрировался на мероприятие «{eventEntity.Title}».",
                Link = $"/Events/Details/{eventEntity.Id}",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Вы успешно зарегистрировались на мероприятие";

            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(int eventId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var participation = await _context.Participations
                .FirstOrDefaultAsync(p => p.EventId == eventId && p.UserId == currentUser.Id);

            if (participation == null)
            {
                TempData["ErrorMessage"] = "Вы не зарегистрированы на это мероприятие";
                return RedirectToAction("Details", "Events", new { id = eventId });
            }

            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventEntity == null)
            {
                return NotFound();
            }

            if (eventEntity.EventDate <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Нельзя отменить участие в прошедшем мероприятии";
                return RedirectToAction("Details", "Events", new { id = eventId });
            }

            _context.Participations.Remove(participation);

            _context.Notifications.Add(new Notification
            {
                UserId = eventEntity.CreatorId,
                Title = "Участник отменил участие",
                Message = $"{currentUser.FirstName} {currentUser.LastName} отменил участие в мероприятии «{eventEntity.Title}».",
                Link = $"/Events/Details/{eventEntity.Id}",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Вы отменили участие в мероприятии";

            return RedirectToAction("Details", "Events", new { id = eventId });
        }
    }
}