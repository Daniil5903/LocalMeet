using LocalMeet.Data;
using LocalMeet.Models.Entities;
using LocalMeet.Models.Enums;
using LocalMeet.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalMeet.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AppRole.Admin)]
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(EventStatus? status, string? searchQuery, string? sortOrder, int page = 1)
        {
            const int pageSize = 10;
            page = Math.Max(page, 1);

            ViewData["CurrentStatus"] = status;
            ViewData["CurrentSearch"] = searchQuery;
            ViewData["CurrentSort"] = sortOrder;

            ViewData["DateSort"] = sortOrder == "date" ? "date_desc" : "date";
            ViewData["CreatedSort"] = sortOrder == "created" ? "created_desc" : "created";
            ViewData["TitleSort"] = sortOrder == "title" ? "title_desc" : "title";

            var query = _context.Events
                .Include(e => e.Category)
                .Include(e => e.Creator)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(e => e.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var normalizedQuery = searchQuery.Trim();

                query = query.Where(e =>
                    e.Title.Contains(normalizedQuery) ||
                    e.Description.Contains(normalizedQuery) ||
                    e.Address.Contains(normalizedQuery));
            }

            query = sortOrder switch
            {
                "date" => query.OrderBy(e => e.EventDate),
                "date_desc" => query.OrderByDescending(e => e.EventDate),

                "created" => query.OrderBy(e => e.CreatedAt),
                "created_desc" => query.OrderByDescending(e => e.CreatedAt),

                "title" => query.OrderBy(e => e.Title),
                "title_desc" => query.OrderByDescending(e => e.Title),

                _ => query
                    .OrderBy(e => e.Status != EventStatus.Pending)
                    .ThenByDescending(e => e.CreatedAt)
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            ViewData["PageNumber"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalItems"] = totalItems;

            var events = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new AdminEventListItemViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    CategoryName = e.Category != null ? e.Category.Name : "Без категории",
                    CreatorName = e.Creator != null
                        ? e.Creator.FirstName + " " + e.Creator.LastName
                        : "Неизвестный пользователь",
                    CreatorEmail = e.Creator != null && e.Creator.Email != null
                        ? e.Creator.Email
                        : "",
                    EventDate = e.EventDate,
                    CreatedAt = e.CreatedAt,
                    Status = e.Status,
                    StatusText = GetStatusText(e.Status),
                    StatusCssClass = GetStatusCssClass(e.Status)
                })
                .ToListAsync();

            return View(events);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null)
            {
                return NotFound();
            }

            eventEntity.Status = EventStatus.Approved;
            eventEntity.RejectReason = null;

            _context.Notifications.Add(new Notification
            {
                UserId = eventEntity.CreatorId,
                Title = "Мероприятие одобрено",
                Message = $"Ваше мероприятие «{eventEntity.Title}» прошло модерацию и опубликовано в каталоге.",
                Link = $"/Events/Details/{eventEntity.Id}",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Мероприятие успешно одобрено";

            return RedirectToAction(nameof(Index), new { status = EventStatus.Pending });
        }

        [HttpGet]
        public async Task<IActionResult> Reject(int id)
        {
            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null)
            {
                return NotFound();
            }

            var model = new RejectEventViewModel
            {
                EventId = eventEntity.Id,
                EventTitle = eventEntity.Title,
                RejectReason = eventEntity.RejectReason ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(RejectEventViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == model.EventId);

            if (eventEntity == null)
            {
                return NotFound();
            }

            eventEntity.Status = EventStatus.Rejected;
            eventEntity.RejectReason = model.RejectReason.Trim();

            _context.Notifications.Add(new Notification
            {
                UserId = eventEntity.CreatorId,
                Title = "Мероприятие отклонено",
                Message = $"Ваше мероприятие «{eventEntity.Title}» было отклонено. Причина: {eventEntity.RejectReason}",
                Link = $"/Events/Details/{eventEntity.Id}",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Мероприятие отклонено";

            return RedirectToAction(nameof(Index), new { status = EventStatus.Pending });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendToModeration(int id)
        {
            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventEntity == null)
            {
                return NotFound();
            }

            eventEntity.Status = EventStatus.Pending;
            eventEntity.RejectReason = null;

            _context.Notifications.Add(new Notification
            {
                UserId = eventEntity.CreatorId,
                Title = "Мероприятие возвращено на модерацию",
                Message = $"Мероприятие «{eventEntity.Title}» снова отправлено на проверку.",
                Link = $"/Events/Details/{eventEntity.Id}",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Мероприятие возвращено на модерацию";

            return RedirectToAction(nameof(Index));
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