using LocalMeet.Data;
using LocalMeet.Models.Entities;
using LocalMeet.Models.Enums;
using LocalMeet.Models.ViewModels.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LocalMeet.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public EventsController(
            ApplicationDbContext context,
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchQuery,
            int? categoryId,
            DateTime? eventDate,
            string? eventPeriod,
            string? sortOrder,
            int page = 1)
        {
            const int pageSize = 6;

            page = Math.Max(page, 1);

            var currentUser = await _userManager.GetUserAsync(User);

            var favoriteEventIds = new HashSet<int>();

            if (currentUser != null)
            {
                var favoriteIds = await _context.FavoriteEvents
                    .Where(f => f.UserId == currentUser.Id)
                    .Select(f => f.EventId)
                    .ToListAsync();

                favoriteEventIds = favoriteIds.ToHashSet();
            }

            var normalizedEventPeriod = NormalizeEventPeriod(eventPeriod);

            var now = DateTime.Now;

            var query = _context.Events
                .Include(e => e.Category)
                .Include(e => e.Creator)
                .Include(e => e.Participations)
                .Where(e => e.Status == EventStatus.Approved)
                .AsQueryable();

            query = normalizedEventPeriod switch
            {
                "past" => query.Where(e => e.EventDate < now),
                "all" => query,
                _ => query.Where(e => e.EventDate >= now)
            };

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var normalizedQuery = searchQuery.Trim();

                query = query.Where(e =>
                    e.Title.Contains(normalizedQuery) ||
                    e.Description.Contains(normalizedQuery) ||
                    e.Address.Contains(normalizedQuery));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(e => e.CategoryId == categoryId.Value);
            }

            if (eventDate.HasValue)
            {
                var date = eventDate.Value.Date;

                query = query.Where(e => e.EventDate.Date == date);
            }

            query = sortOrder switch
            {
                "date_desc" => query.OrderByDescending(e => e.EventDate),
                "created" => query.OrderBy(e => e.CreatedAt),
                "created_desc" => query.OrderByDescending(e => e.CreatedAt),
                "title" => query.OrderBy(e => e.Title),
                "title_desc" => query.OrderByDescending(e => e.Title),
                _ => normalizedEventPeriod == "past"
                    ? query.OrderByDescending(e => e.EventDate)
                    : query.OrderBy(e => e.EventDate)
            };

            var totalItems = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var eventEntities = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var events = eventEntities
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

            var model = new EventCatalogViewModel
            {
                Events = events,
                SearchQuery = searchQuery,
                CategoryId = categoryId,
                EventDate = eventDate,
                EventPeriod = normalizedEventPeriod,
                SortOrder = sortOrder,
                PageNumber = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                Categories = await GetCategorySelectListAsync(categoryId)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var eventEntity = await _context.Events
                .Include(e => e.Category)
                .Include(e => e.Creator)
                .Include(e => e.Participations)
                    .ThenInclude(p => p.User)
                .Include(e => e.Messages)
                    .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            var isAuthenticated = currentUser != null;

            var isAdmin =
                currentUser != null &&
                await _userManager.IsInRoleAsync(
                    currentUser,
                    AppRole.Admin);

            var isCreator =
                currentUser != null &&
                eventEntity.CreatorId == currentUser.Id;

            if (eventEntity.Status != EventStatus.Approved &&
                !isCreator &&
                !isAdmin)
            {
                return Forbid();
            }

            var isParticipant =
                currentUser != null &&
                eventEntity.Participations
                    .Any(p => p.UserId == currentUser.Id);

            var participantsCount =
                eventEntity.Participations.Count;

            var canJoin =
                currentUser != null &&
                !currentUser.IsBlocked &&
                eventEntity.Status == EventStatus.Approved &&
                !isCreator &&
                !isParticipant &&
                participantsCount < eventEntity.MaxParticipants &&
                eventEntity.EventDate > DateTime.Now;

            var canLeave =
                currentUser != null &&
                isParticipant &&
                eventEntity.Status == EventStatus.Approved &&
                eventEntity.EventDate > DateTime.Now;

            var isFavorite =
                currentUser != null &&
                await _context.FavoriteEvents
                    .AnyAsync(f =>
                        f.UserId == currentUser.Id &&
                        f.EventId == eventEntity.Id);

            var canToggleFavorite =
                currentUser != null &&
                !currentUser.IsBlocked &&
                eventEntity.Status == EventStatus.Approved;

            var canUseChat =
                currentUser != null &&
                (!currentUser.IsBlocked || isAdmin) &&
                (
                    isAdmin ||
                    (
                        eventEntity.Status == EventStatus.Approved &&
                        (isCreator || isParticipant)
                    )
                );

            var canReportEvent =
                currentUser != null &&
                !currentUser.IsBlocked &&
                !isCreator &&
                !isAdmin;

            var model = new EventDetailsViewModel
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                Address = eventEntity.Address,
                Latitude = eventEntity.Latitude,
                Longitude = eventEntity.Longitude,
                EventDate = eventEntity.EventDate,
                CreatedAt = eventEntity.CreatedAt,
                MaxParticipants = eventEntity.MaxParticipants,
                ParticipantsCount = participantsCount,
                Status = eventEntity.Status,
                StatusText = GetStatusText(eventEntity.Status),
                StatusCssClass = GetStatusCssClass(eventEntity.Status),
                RejectReason = eventEntity.RejectReason,
                CategoryName = eventEntity.Category?.Name ?? "Без категории",
                CreatorId = eventEntity.CreatorId,
                CreatorName = eventEntity.Creator != null
                    ? eventEntity.Creator.FirstName + " " + eventEntity.Creator.LastName
                    : "Неизвестный пользователь",
                CreatorAvatarPath = eventEntity.Creator?.AvatarPath,
                CurrentUserId = currentUser?.Id,
                CanManage = isCreator || isAdmin,
                IsAuthenticated = isAuthenticated,
                IsCreator = isCreator,
                IsParticipant = isParticipant,
                CanJoin = canJoin,
                CanLeave = canLeave,
                IsFavorite = isFavorite,
                CanToggleFavorite = canToggleFavorite,
                CanUseChat = canUseChat,
                IsAdmin = isAdmin,
                CanReportEvent = canReportEvent,

                Participants = eventEntity.Participations
                    .OrderBy(p => p.CreatedAt)
                    .Where(p => p.User != null)
                    .Select(p => new EventParticipantViewModel
                    {
                        UserId = p.UserId,
                        FullName = p.User!.FirstName + " " + p.User.LastName,
                        AvatarPath = p.User.AvatarPath,
                        JoinedAt = p.CreatedAt
                    })
                    .ToList(),

                ChatMessages = eventEntity.Messages
                    .OrderBy(m => m.CreatedAt)
                    .Where(m => m.User != null)
                    .Select(m => new EventChatMessageViewModel
                    {
                        Id = m.Id,
                        UserId = m.UserId,
                        AuthorName = m.User!.FirstName + " " + m.User.LastName,
                        AuthorAvatarPath = m.User.AvatarPath,
                        Text = m.Text,
                        CreatedAt = m.CreatedAt,
                        IsDeleted = m.IsDeleted,
                        CanReport =
                            currentUser != null &&
                            !currentUser.IsBlocked &&
                            !isAdmin &&
                            !m.IsDeleted &&
                            m.UserId != currentUser.Id
                    })
                    .ToList()
            };

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Create()
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (currentUser.IsBlocked)
            {
                TempData["ErrorMessage"] =
                    "Заблокированный пользователь не может создавать мероприятия";

                return RedirectToAction(nameof(Index));
            }

            var model = new CreateEventViewModel
            {
                Categories = await GetCategorySelectListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateEventViewModel model)
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (currentUser.IsBlocked)
            {
                TempData["ErrorMessage"] =
                    "Заблокированный пользователь не может создавать мероприятия";

                return RedirectToAction(nameof(Index));
            }

            if (model.EventDate <= DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(model.EventDate),
                    "Дата мероприятия должна быть позже текущего времени");
            }

            var categoryExists =
                await _context.Categories
                    .AnyAsync(c => c.Id == model.CategoryId);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId),
                    "Выбранная категория не найдена");
            }

            if (!model.Latitude.HasValue ||
                !model.Longitude.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Выберите место проведения мероприятия на карте");
            }

            if (!ModelState.IsValid)
            {
                model.Categories =
                    await GetCategorySelectListAsync(
                        model.CategoryId);

                return View(model);
            }

            var eventEntity = new Event
            {
                Title = model.Title.Trim(),
                Description = model.Description.Trim(),
                Address = model.Address.Trim(),
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                EventDate = model.EventDate,
                MaxParticipants = model.MaxParticipants,
                CategoryId = model.CategoryId,
                CreatorId = currentUser.Id,
                Status = EventStatus.Pending,
                CreatedAt = DateTime.Now
            };

            _context.Events.Add(eventEntity);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Мероприятие создано и отправлено на модерацию";

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = eventEntity.Id
                });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var eventEntity =
                await _context.Events
                    .Include(e => e.Category)
                    .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null)
            {
                return NotFound();
            }

            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var isAdmin =
                await _userManager.IsInRoleAsync(
                    currentUser,
                    AppRole.Admin);

            var isCreator =
                eventEntity.CreatorId == currentUser.Id;

            if (!isCreator && !isAdmin)
            {
                return Forbid();
            }

            if (currentUser.IsBlocked && !isAdmin)
            {
                TempData["ErrorMessage"] =
                    "Заблокированный пользователь не может редактировать мероприятия";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = eventEntity.Id
                    });
            }

            var model = new EditEventViewModel
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                Description = eventEntity.Description,
                Address = eventEntity.Address,
                Latitude = eventEntity.Latitude,
                Longitude = eventEntity.Longitude,
                EventDate = eventEntity.EventDate,
                MaxParticipants = eventEntity.MaxParticipants,
                CategoryId = eventEntity.CategoryId,
                Status = eventEntity.Status,
                Categories =
                    await GetCategorySelectListAsync(
                        eventEntity.CategoryId)
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            EditEventViewModel model)
        {
            var eventEntity =
                await _context.Events
                    .Include(e => e.Participations)
                    .FirstOrDefaultAsync(e => e.Id == model.Id);

            if (eventEntity == null)
            {
                return NotFound();
            }

            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var isAdmin =
                await _userManager.IsInRoleAsync(
                    currentUser,
                    AppRole.Admin);

            var isCreator =
                eventEntity.CreatorId == currentUser.Id;

            if (!isCreator && !isAdmin)
            {
                return Forbid();
            }

            if (currentUser.IsBlocked && !isAdmin)
            {
                TempData["ErrorMessage"] =
                    "Заблокированный пользователь не может редактировать мероприятия";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = eventEntity.Id
                    });
            }

            if (model.EventDate <= DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(model.EventDate),
                    "Дата мероприятия должна быть позже текущего времени");
            }

            var categoryExists =
                await _context.Categories
                    .AnyAsync(c => c.Id == model.CategoryId);

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    nameof(model.CategoryId),
                    "Выбранная категория не найдена");
            }

            if (!model.Latitude.HasValue ||
                !model.Longitude.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Выберите место проведения мероприятия на карте");
            }

            var participantsCount =
                eventEntity.Participations.Count;

            if (model.MaxParticipants < participantsCount)
            {
                ModelState.AddModelError(
                    nameof(model.MaxParticipants),
                    $"Лимит участников не может быть меньше текущего количества участников: {participantsCount}");
            }

            if (!ModelState.IsValid)
            {
                model.Categories =
                    await GetCategorySelectListAsync(
                        model.CategoryId);

                model.Status =
                    eventEntity.Status;

                return View(model);
            }

            eventEntity.Title =
                model.Title.Trim();

            eventEntity.Description =
                model.Description.Trim();

            eventEntity.Address =
                model.Address.Trim();

            eventEntity.Latitude =
                model.Latitude;

            eventEntity.Longitude =
                model.Longitude;

            eventEntity.EventDate =
                model.EventDate;

            eventEntity.MaxParticipants =
                model.MaxParticipants;

            eventEntity.CategoryId =
                model.CategoryId;

            if (!isAdmin)
            {
                eventEntity.Status =
                    EventStatus.Pending;

                eventEntity.RejectReason =
                    null;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                isAdmin
                    ? "Мероприятие успешно обновлено"
                    : "Мероприятие обновлено и повторно отправлено на модерацию";

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = eventEntity.Id
                });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var eventEntity =
                await _context.Events
                    .Include(e => e.Category)
                    .Include(e => e.Participations)
                    .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null)
            {
                return NotFound();
            }

            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var isAdmin =
                await _userManager.IsInRoleAsync(
                    currentUser,
                    AppRole.Admin);

            var isCreator =
                eventEntity.CreatorId == currentUser.Id;

            if (!isCreator && !isAdmin)
            {
                return Forbid();
            }

            var model = new DeleteEventViewModel
            {
                Id = eventEntity.Id,
                Title = eventEntity.Title,
                CategoryName =
                    eventEntity.Category?.Name ??
                    "Без категории",
                EventDate = eventEntity.EventDate,
                ParticipantsCount =
                    eventEntity.Participations.Count,
                Status = eventEntity.Status,
                StatusText =
                    GetStatusText(eventEntity.Status),
                StatusCssClass =
                    GetStatusCssClass(eventEntity.Status)
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var eventEntity =
                await _context.Events
                    .Include(e => e.Participations)
                    .Include(e => e.FavoriteEvents)
                    .Include(e => e.Messages)
                    .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null)
            {
                return NotFound();
            }

            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var isAdmin =
                await _userManager.IsInRoleAsync(
                    currentUser,
                    AppRole.Admin);

            var isCreator =
                eventEntity.CreatorId == currentUser.Id;

            if (!isCreator && !isAdmin)
            {
                return Forbid();
            }

            if (currentUser.IsBlocked && !isAdmin)
            {
                TempData["ErrorMessage"] =
                    "Заблокированный пользователь не может удалять мероприятия";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        id = eventEntity.Id
                    });
            }

            if (eventEntity.Messages.Any())
            {
                _context.EventMessages.RemoveRange(
                    eventEntity.Messages);
            }

            if (eventEntity.Participations.Any())
            {
                _context.Participations.RemoveRange(
                    eventEntity.Participations);
            }

            if (eventEntity.FavoriteEvents.Any())
            {
                _context.FavoriteEvents.RemoveRange(
                    eventEntity.FavoriteEvents);
            }

            _context.Events.Remove(eventEntity);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Мероприятие успешно удалено";

            if (isAdmin)
            {
                return RedirectToAction(
                    "Index",
                    "Events",
                    new
                    {
                        area = "Admin"
                    });
            }

            return RedirectToAction(
                "Index",
                "MyEvents");
        }

        private async Task<IEnumerable<SelectListItem>>
            GetCategorySelectListAsync(
                int? selectedCategoryId = null)
        {
            var categories =
                await _context.Categories
                    .OrderBy(c => c.Name)
                    .ToListAsync();

            return categories.Select(c =>
                new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected =
                        selectedCategoryId.HasValue &&
                        c.Id == selectedCategoryId.Value
                });
        }

        private static string NormalizeEventPeriod(
            string? eventPeriod)
        {
            if (string.IsNullOrWhiteSpace(eventPeriod))
            {
                return "upcoming";
            }

            var normalized =
                eventPeriod.Trim().ToLowerInvariant();

            return normalized switch
            {
                "past" => "past",
                "all" => "all",
                _ => "upcoming"
            };
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