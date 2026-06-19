using LocalMeet.Data;
using LocalMeet.Models.Entities;
using LocalMeet.Models.Enums;
using LocalMeet.Models.ViewModels.Events;
using LocalMeet.Models.ViewModels.Search;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalMeet.Controllers
{
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public SearchController(
            ApplicationDbContext context,
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? query)
        {
            var model = new SearchViewModel
            {
                Query = query
            };

            if (string.IsNullOrWhiteSpace(query))
            {
                return View(model);
            }

            var normalizedQuery = query.Trim();
            var currentUser = await _userManager.GetUserAsync(User);
            var favoriteEventIds = await GetFavoriteEventIdsAsync(currentUser);

            var eventEntities = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Creator)
                .Include(e => e.Participations)
                .Where(e =>
                    e.Status == EventStatus.Approved &&
                    (e.Title.Contains(normalizedQuery) ||
                     e.Description.Contains(normalizedQuery) ||
                     e.Address.Contains(normalizedQuery) ||
                     (e.Category != null && e.Category.Name.Contains(normalizedQuery))))
                .OrderBy(e => e.EventDate)
                .Take(12)
                .ToListAsync();

            model.Events = eventEntities
                .Select(e => new EventListItemViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    DescriptionPreview = e.Description.Length > 160
                        ? e.Description.Substring(0, 160) + "..."
                        : e.Description,
                    Address = e.Address,
                    EventDate = e.EventDate,
                    CreatedAt = e.CreatedAt,
                    MaxParticipants = e.MaxParticipants,
                    ParticipantsCount = e.Participations.Count,
                    Status = e.Status,
                    StatusText = GetStatusText(e.Status),
                    StatusCssClass = GetStatusCssClass(e.Status),
                    CategoryName = e.Category != null ? e.Category.Name : "Без категории",
                    CreatorName = e.Creator != null
                        ? e.Creator.FirstName + " " + e.Creator.LastName
                        : "Неизвестный пользователь",
                    IsFavorite = favoriteEventIds.Contains(e.Id),
                    CanToggleFavorite = currentUser != null && !currentUser.IsBlocked
                })
                .ToList();

            model.Users = await _context.Users
                .Where(u =>
                    !u.IsBlocked &&
                    (u.FirstName.Contains(normalizedQuery) ||
                     u.LastName.Contains(normalizedQuery) ||
                     u.Email!.Contains(normalizedQuery)))
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Take(12)
                .Select(u => new SearchUserListItemViewModel
                {
                    Id = u.Id,
                    FullName = u.FirstName + " " + u.LastName,
                    AvatarPath = u.AvatarPath,
                    AboutPreview = u.About,
                    IsPrivateProfile = u.IsPrivateProfile,
                    RegistrationDate = u.RegistrationDate,
                    CreatedEventsCount = u.CreatedEvents.Count,
                    ParticipationsCount = u.Participations.Count
                })
                .ToListAsync();

            foreach (var user in model.Users.Where(u => u.AboutPreview != null && u.AboutPreview.Length > 120))
            {
                user.AboutPreview = user.AboutPreview![..120] + "...";
            }

            return View(model);
        }

        private async Task<HashSet<int>> GetFavoriteEventIdsAsync(User? currentUser)
        {
            if (currentUser == null)
            {
                return new HashSet<int>();
            }

            var favoriteIds = await _context.FavoriteEvents
                .Where(f => f.UserId == currentUser.Id)
                .Select(f => f.EventId)
                .ToListAsync();

            return favoriteIds.ToHashSet();
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