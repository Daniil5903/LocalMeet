using LocalMeet.Data;
using LocalMeet.Models.Entities;
using LocalMeet.Models.Enums;
using LocalMeet.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalMeet.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AppRole.Admin)]
    public class UserManagementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public UserManagementController(
            ApplicationDbContext context,
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchQuery,
            string? roleFilter,
            string? statusFilter,
            string? sortOrder,
            int page = 1)
        {
            const int pageSize = 10;
            page = Math.Max(page, 1);

            ViewData["NameSort"] = sortOrder == "name" ? "name_desc" : "name";
            ViewData["DateSort"] = sortOrder == "date" ? "date_desc" : "date";
            ViewData["LastVisitSort"] = sortOrder == "last_visit" ? "last_visit_desc" : "last_visit";
            ViewData["EventsSort"] = sortOrder == "events" ? "events_desc" : "events";

            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var normalizedQuery = searchQuery.Trim();

                query = query.Where(u =>
                    u.Email!.Contains(normalizedQuery) ||
                    u.FirstName.Contains(normalizedQuery) ||
                    u.LastName.Contains(normalizedQuery));
            }

            if (statusFilter == "active")
            {
                query = query.Where(u => !u.IsBlocked);
            }
            else if (statusFilter == "blocked")
            {
                query = query.Where(u => u.IsBlocked);
            }

            query = sortOrder switch
            {
                "name" => query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName),
                "name_desc" => query.OrderByDescending(u => u.LastName).ThenByDescending(u => u.FirstName),
                "date" => query.OrderBy(u => u.RegistrationDate),
                "date_desc" => query.OrderByDescending(u => u.RegistrationDate),
                "last_visit" => query.OrderBy(u => u.LastVisit),
                "last_visit_desc" => query.OrderByDescending(u => u.LastVisit),
                "events" => query.OrderBy(u => u.CreatedEvents.Count),
                "events_desc" => query.OrderByDescending(u => u.CreatedEvents.Count),
                _ => query.OrderByDescending(u => u.RegistrationDate)
            };

            var users = await query
                .Select(u => new UserManagementListItemViewModel
                {
                    Id = u.Id,
                    FullName = u.FirstName + " " + u.LastName,
                    Email = u.Email ?? "",
                    IsBlocked = u.IsBlocked,
                    IsPrivateProfile = u.IsPrivateProfile,
                    RegistrationDate = u.RegistrationDate,
                    LastVisit = u.LastVisit,
                    CreatedEventsCount = u.CreatedEvents.Count,
                    ParticipationsCount = u.Participations.Count
                })
                .ToListAsync();

            foreach (var user in users)
            {
                var entity = await _userManager.FindByIdAsync(user.Id);
                user.IsAdmin = entity != null && await _userManager.IsInRoleAsync(entity, AppRole.Admin);
            }

            if (roleFilter == AppRole.Admin)
            {
                users = users.Where(u => u.IsAdmin).ToList();
            }
            else if (roleFilter == AppRole.User)
            {
                users = users.Where(u => !u.IsAdmin).ToList();
            }

            var totalItems = users.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var pagedUsers = users
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var model = new UserManagementIndexViewModel
            {
                SearchQuery = searchQuery,
                RoleFilter = roleFilter,
                StatusFilter = statusFilter,
                SortOrder = sortOrder,
                TotalUsersCount = await _context.Users.CountAsync(),
                ActiveUsersCount = await _context.Users.CountAsync(u => !u.IsBlocked),
                BlockedUsersCount = await _context.Users.CountAsync(u => u.IsBlocked),
                AdminsCount = users.Count(u => u.IsAdmin),
                Users = pagedUsers,
                PageNumber = page,
                TotalPages = totalPages,
                TotalItems = totalItems
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var model = await BuildDetailsViewModelAsync(user);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Block(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);

            if (user.Id == currentUserId)
            {
                TempData["ErrorMessage"] = "Нельзя заблокировать собственную учетную запись";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (await _userManager.IsInRoleAsync(user, AppRole.Admin))
            {
                TempData["ErrorMessage"] = "Нельзя заблокировать администратора";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!user.IsBlocked)
            {
                user.IsBlocked = true;
                await _userManager.UpdateAsync(user);

                _context.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Title = "Учетная запись заблокирована",
                    Message = "Ваша учетная запись была заблокирована администратором.",
                    Link = null,
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Пользователь заблокирован";

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unblock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            if (user.IsBlocked)
            {
                user.IsBlocked = false;
                await _userManager.UpdateAsync(user);

                _context.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Title = "Учетная запись разблокирована",
                    Message = "Ваша учетная запись была разблокирована администратором.",
                    Link = null,
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Пользователь разблокирован";

            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<UserManagementDetailsViewModel> BuildDetailsViewModelAsync(User user)
        {
            var isAdmin = await _userManager.IsInRoleAsync(user, AppRole.Admin);

            var recentCreatedEvents = await _context.Events
                .Where(e => e.CreatorId == user.Id)
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentParticipations = await _context.Participations
                .Where(p => p.UserId == user.Id)
                .Include(p => p.Event)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            return new UserManagementDetailsViewModel
            {
                Id = user.Id,
                FullName = user.FirstName + " " + user.LastName,
                Email = user.Email ?? "",
                AvatarPath = user.AvatarPath,
                About = user.About,
                IsBlocked = user.IsBlocked,
                IsAdmin = isAdmin,
                IsPrivateProfile = user.IsPrivateProfile,
                RegistrationDate = user.RegistrationDate,
                LastVisit = user.LastVisit,
                CreatedEventsCount = await _context.Events.CountAsync(e => e.CreatorId == user.Id),
                ParticipationsCount = await _context.Participations.CountAsync(p => p.UserId == user.Id),
                FavoriteEventsCount = await _context.FavoriteEvents.CountAsync(f => f.UserId == user.Id),
                ReportsCreatedCount = await _context.Reports.CountAsync(r => r.AuthorId == user.Id),
                MessagesCount = await _context.EventMessages.CountAsync(m => m.UserId == user.Id && !m.IsDeleted),
                RecentCreatedEvents = recentCreatedEvents
                    .Select(e => new UserManagementEventItemViewModel
                    {
                        Id = e.Id,
                        Title = e.Title,
                        EventDate = e.EventDate,
                        StatusText = GetEventStatusText(e.Status),
                        StatusCssClass = GetEventStatusCssClass(e.Status)
                    })
                    .ToList(),
                RecentParticipations = recentParticipations
                    .Select(p => new UserManagementEventItemViewModel
                    {
                        Id = p.EventId,
                        Title = p.Event == null ? "Мероприятие не найдено" : p.Event.Title,
                        EventDate = p.Event == null ? DateTime.MinValue : p.Event.EventDate,
                        StatusText = p.Event == null ? "Неизвестно" : GetEventStatusText(p.Event.Status),
                        StatusCssClass = p.Event == null ? "bg-secondary" : GetEventStatusCssClass(p.Event.Status)
                    })
                    .ToList()
            };
        }

        private static string GetEventStatusText(EventStatus status)
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

        private static string GetEventStatusCssClass(EventStatus status)
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