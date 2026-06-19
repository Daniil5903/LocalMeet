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
    public class FavoritesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public FavoritesController(
            ApplicationDbContext context,
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var favorites = await _context.FavoriteEvents
                .Include(f => f.Event)
                    .ThenInclude(e => e!.Category)
                .Include(f => f.Event)
                    .ThenInclude(e => e!.Creator)
                .Include(f => f.Event)
                    .ThenInclude(e => e!.Participations)
                .Where(f => f.UserId == currentUser.Id)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            var model = favorites
                .Where(f => f.Event != null)
                .Select(f => MapToEventListItem(f.Event!, currentUser))
                .ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int eventId, string? returnUrl)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (currentUser.IsBlocked)
            {
                TempData["ErrorMessage"] = "Заблокированный пользователь не может добавлять мероприятия в избранное";
                return RedirectBack(returnUrl, eventId);
            }

            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventEntity == null)
            {
                return NotFound();
            }

            if (eventEntity.Status != EventStatus.Approved)
            {
                TempData["ErrorMessage"] = "В избранное можно добавлять только опубликованные мероприятия";
                return RedirectBack(returnUrl, eventId);
            }

            var alreadyExists = await _context.FavoriteEvents
                .AnyAsync(f => f.UserId == currentUser.Id && f.EventId == eventId);

            if (!alreadyExists)
            {
                var favorite = new FavoriteEvent
                {
                    UserId = currentUser.Id,
                    EventId = eventId,
                    CreatedAt = DateTime.Now
                };

                _context.FavoriteEvents.Add(favorite);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Мероприятие добавлено в избранное";
            }

            return RedirectBack(returnUrl, eventId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int eventId, string? returnUrl)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var favorite = await _context.FavoriteEvents
                .FirstOrDefaultAsync(f => f.UserId == currentUser.Id && f.EventId == eventId);

            if (favorite != null)
            {
                _context.FavoriteEvents.Remove(favorite);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Мероприятие удалено из избранного";
            }

            return RedirectBack(returnUrl, eventId);
        }

        private IActionResult RedirectBack(string? returnUrl, int eventId)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        private static EventListItemViewModel MapToEventListItem(Event eventEntity, User currentUser)
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
                    : "Неизвестный пользователь",
                IsFavorite = true,
                CanToggleFavorite = !currentUser.IsBlocked && eventEntity.Status == EventStatus.Approved
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