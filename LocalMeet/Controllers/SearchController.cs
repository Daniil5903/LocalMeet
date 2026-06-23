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

            var currentUser =
                await _userManager.GetUserAsync(User);

            var currentUserId =
                currentUser?.Id;

            var isAdmin =
                currentUser != null &&
                await _userManager.IsInRoleAsync(
                    currentUser,
                    AppRole.Admin);

            var favoriteEventIds =
                await GetFavoriteEventIdsAsync(
                    currentUser);

            var eventEntities =
                await _context.Events
                    .AsNoTracking()
                    .Include(e => e.Category)
                    .Include(e => e.Creator)
                    .Include(e => e.Participations)
                    .Where(e =>
                        e.Status == EventStatus.Approved &&
                        (
                            e.Title.Contains(normalizedQuery) ||
                            e.Description.Contains(normalizedQuery) ||
                            e.Address.Contains(normalizedQuery) ||
                            (
                                e.Category != null &&
                                e.Category.Name.Contains(normalizedQuery)
                            )
                        ))
                    .OrderBy(e => e.EventDate)
                    .Take(12)
                    .ToListAsync();

            model.Events =
                eventEntities
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
                        CanViewStatus = false,
                        CategoryName = e.Category != null
                            ? e.Category.Name
                            : "Без категории",
                        CreatorName = e.Creator != null
                            ? e.Creator.FirstName + " " + e.Creator.LastName
                            : "Неизвестный пользователь",
                        IsFavorite = favoriteEventIds.Contains(e.Id),
                        CanToggleFavorite =
                            currentUser != null &&
                            !currentUser.IsBlocked
                    })
                    .ToList();

            var usersQuery =
                _context.Users
                    .AsNoTracking()
                    .Where(u => !u.IsBlocked);

            if (isAdmin)
            {
                usersQuery =
                    usersQuery.Where(u =>
                        u.FirstName.Contains(normalizedQuery) ||
                        u.LastName.Contains(normalizedQuery) ||
                        (
                            u.Email != null &&
                            u.Email.Contains(normalizedQuery)
                        ) ||
                        (
                            u.About != null &&
                            u.About.Contains(normalizedQuery)
                        ));
            }
            else
            {
                usersQuery =
                    usersQuery.Where(u =>
                        u.FirstName.Contains(normalizedQuery) ||
                        u.LastName.Contains(normalizedQuery) ||
                        (
                            !u.IsPrivateProfile &&
                            u.About != null &&
                            u.About.Contains(normalizedQuery)
                        ));
            }

            model.Users =
                await usersQuery
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .Take(12)
                    .Select(u => new SearchUserListItemViewModel
                    {
                        Id = u.Id,
                        FullName = u.FirstName + " " + u.LastName,
                        AvatarPath = u.AvatarPath,
                        AboutPreview =
                            isAdmin ||
                            (
                                currentUserId != null &&
                                u.Id == currentUserId
                            ) ||
                            !u.IsPrivateProfile
                                ? u.About
                                : null,
                        IsPrivateProfile = u.IsPrivateProfile,
                        CanViewPrivateInfo =
                            isAdmin ||
                            (
                                currentUserId != null &&
                                u.Id == currentUserId
                            ) ||
                            !u.IsPrivateProfile,
                        RegistrationDate = u.RegistrationDate,
                        CreatedEventsCount = u.CreatedEvents.Count,
                        ParticipationsCount = u.Participations.Count
                    })
                    .ToListAsync();

            foreach (var user in model.Users
                         .Where(u =>
                             u.AboutPreview != null &&
                             u.AboutPreview.Length > 120))
            {
                user.AboutPreview =
                    user.AboutPreview![..120] + "...";
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Autocomplete(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new
                {
                    items = Array.Empty<object>(),
                    totalCount = 0
                });
            }

            var normalizedQuery = query.Trim();

            if (normalizedQuery.Length < 2)
            {
                return Json(new
                {
                    items = Array.Empty<object>(),
                    totalCount = 0
                });
            }

            var currentUser =
                await _userManager.GetUserAsync(User);

            var currentUserId =
                currentUser?.Id;

            var isAdmin =
                currentUser != null &&
                await _userManager.IsInRoleAsync(
                    currentUser,
                    AppRole.Admin);

            var items = new List<object>();

            var events =
                await _context.Events
                    .AsNoTracking()
                    .Include(e => e.Category)
                    .Where(e =>
                        e.Status == EventStatus.Approved &&
                        (
                            e.Title.Contains(normalizedQuery) ||
                            e.Description.Contains(normalizedQuery) ||
                            e.Address.Contains(normalizedQuery) ||
                            (
                                e.Category != null &&
                                e.Category.Name.Contains(normalizedQuery)
                            )
                        ))
                    .OrderBy(e => e.EventDate)
                    .Take(5)
                    .Select(e => new
                    {
                        e.Id,
                        e.Title,
                        e.EventDate,
                        e.Address,
                        CategoryName = e.Category != null
                            ? e.Category.Name
                            : "Без категории"
                    })
                    .ToListAsync();

            foreach (var eventItem in events)
            {
                items.Add(new
                {
                    type = "event",
                    typeText = "Мероприятие",
                    title = eventItem.Title,
                    subtitle =
                        $"{eventItem.CategoryName} • {eventItem.EventDate:dd.MM.yyyy HH:mm}",
                    meta = eventItem.Address,
                    url =
                        Url.Action(
                            "Details",
                            "Events",
                            new
                            {
                                id = eventItem.Id
                            }) ??
                        $"/Events/Details/{eventItem.Id}",
                    avatarPath = "",
                    badge = eventItem.CategoryName
                });
            }

            var usersQuery =
                _context.Users
                    .AsNoTracking()
                    .Where(u => !u.IsBlocked);

            if (isAdmin)
            {
                usersQuery =
                    usersQuery.Where(u =>
                        u.FirstName.Contains(normalizedQuery) ||
                        u.LastName.Contains(normalizedQuery) ||
                        (
                            u.Email != null &&
                            u.Email.Contains(normalizedQuery)
                        ) ||
                        (
                            u.About != null &&
                            u.About.Contains(normalizedQuery)
                        ));
            }
            else
            {
                usersQuery =
                    usersQuery.Where(u =>
                        u.FirstName.Contains(normalizedQuery) ||
                        u.LastName.Contains(normalizedQuery) ||
                        (
                            !u.IsPrivateProfile &&
                            u.About != null &&
                            u.About.Contains(normalizedQuery)
                        ));
            }

            var users =
                await usersQuery
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .Take(5)
                    .Select(u => new
                    {
                        u.Id,
                        u.FirstName,
                        u.LastName,
                        u.AvatarPath,
                        u.About,
                        u.IsPrivateProfile
                    })
                    .ToListAsync();

            foreach (var user in users)
            {
                var canViewPrivateInfo =
                    isAdmin ||
                    (
                        currentUserId != null &&
                        user.Id == currentUserId
                    ) ||
                    !user.IsPrivateProfile;

                var subtitle =
                    canViewPrivateInfo
                        ? TrimPreview(
                            string.IsNullOrWhiteSpace(user.About)
                                ? "Пользователь LocalMeet"
                                : user.About,
                            80)
                        : "Профиль пользователя";

                items.Add(new
                {
                    type = "user",
                    typeText = "Пользователь",
                    title = $"{user.FirstName} {user.LastName}",
                    subtitle,
                    meta = "",
                    url =
                        Url.Action(
                            "ViewUser",
                            "Profile",
                            new
                            {
                                id = user.Id
                            }) ??
                        $"/Profile/User/{user.Id}",
                    avatarPath = string.IsNullOrWhiteSpace(user.AvatarPath)
                        ? "/images/default-avatar.png"
                        : user.AvatarPath,
                    badge = "Профиль"
                });
            }

            return Json(new
            {
                items,
                totalCount = items.Count
            });
        }

        private async Task<HashSet<int>> GetFavoriteEventIdsAsync(
            User? currentUser)
        {
            if (currentUser == null)
            {
                return new HashSet<int>();
            }

            var favoriteIds =
                await _context.FavoriteEvents
                    .Where(f => f.UserId == currentUser.Id)
                    .Select(f => f.EventId)
                    .ToListAsync();

            return favoriteIds.ToHashSet();
        }

        private static string TrimPreview(
            string text,
            int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var normalizedText = text.Trim();

            return normalizedText.Length > maxLength
                ? normalizedText.Substring(0, maxLength) + "..."
                : normalizedText;
        }

        private static string GetStatusText(
            EventStatus status)
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

        private static string GetStatusCssClass(
            EventStatus status)
        {
            return status switch
            {
                EventStatus.Pending => "text-bg-warning",
                EventStatus.Approved => "text-bg-success",
                EventStatus.Rejected => "text-bg-danger",
                EventStatus.Cancelled => "text-bg-secondary",
                EventStatus.Completed => "text-bg-info",
                _ => "text-bg-light"
            };
        }
    }
}