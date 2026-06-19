using LocalMeet.Data;
using LocalMeet.Models.Entities;
using LocalMeet.Models.Enums;
using LocalMeet.Models.ViewModels.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalMeet.Controllers
{
    [Authorize]
    [Route("MyEvents")]
    public class MyEventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public MyEventsController(
            ApplicationDbContext context,
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var now = DateTime.Now;

            var createdEvents = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Creator)
                .Include(e => e.Participations)
                .Where(e => e.CreatorId == currentUser.Id && e.EventDate >= now)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            var participatingEvents = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Creator)
                .Include(e => e.Participations)
                .Where(e =>
                    e.EventDate >= now &&
                    e.Participations.Any(p => p.UserId == currentUser.Id))
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            var pastEvents = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Creator)
                .Include(e => e.Participations)
                .Where(e =>
                    e.EventDate < now &&
                    (e.CreatorId == currentUser.Id ||
                     e.Participations.Any(p => p.UserId == currentUser.Id)))
                .OrderByDescending(e => e.EventDate)
                .ToListAsync();

            var model = new MyEventsViewModel
            {
                CreatedEvents = createdEvents
                    .Select(MapToEventListItem)
                    .ToList(),

                ParticipatingEvents = participatingEvents
                    .Select(MapToEventListItem)
                    .ToList(),

                PastEvents = pastEvents
                    .Select(MapToEventListItem)
                    .ToList()
            };

            return View(model);
        }

        private static EventListItemViewModel MapToEventListItem(Event eventEntity)
        {
            return new EventListItemViewModel
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                DescriptionPreview = eventEntity.Description.Length > 160
                    ? eventEntity.Description.Substring(0, 160) + "..."
                    : eventEntity.Description,
                Address = eventEntity.Address,
                EventDate = eventEntity.EventDate,
                CreatedAt = eventEntity.CreatedAt,
                MaxParticipants = eventEntity.MaxParticipants,
                ParticipantsCount = eventEntity.Participations.Count,
                Status = eventEntity.Status,
                StatusText = GetStatusText(eventEntity.Status),
                StatusCssClass = GetStatusCssClass(eventEntity.Status),
                CategoryName = eventEntity.Category?.Name ?? "Без категории",
                CreatorName = eventEntity.Creator != null
                    ? eventEntity.Creator.FirstName + " " + eventEntity.Creator.LastName
                    : "Неизвестный пользователь"
            };
        }

        private static string GetStatusText(EventStatus status)
        {
            return status switch
            {
                EventStatus.Pending => "На модерации",
                EventStatus.Approved => "Одобрено",
                EventStatus.Rejected => "Отклонено",
                EventStatus.Cancelled => "Отменено",
                EventStatus.Completed => "Завершено",
                _ => "Неизвестно"
            };
        }

        private static string GetStatusCssClass(EventStatus status)
        {
            return status switch
            {
                EventStatus.Pending => "bg-warning text-dark",
                EventStatus.Approved => "bg-success",
                EventStatus.Rejected => "bg-danger",
                EventStatus.Cancelled => "bg-secondary",
                EventStatus.Completed => "bg-primary",
                _ => "bg-light text-dark"
            };
        }
    }
}