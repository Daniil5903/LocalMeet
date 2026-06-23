using LocalMeet.Data;
using LocalMeet.Models.Entities;
using LocalMeet.Models.Enums;
using LocalMeet.Models.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalMeet.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AppRole.Admin)]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ReportsController(
            ApplicationDbContext context,
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            ReportStatus? status,
            ReportTargetType? targetType,
            string? searchQuery,
            int page = 1)
        {
            const int pageSize = 10;
            page = Math.Max(page, 1);

            ViewData["CurrentStatus"] = status;
            ViewData["CurrentTargetType"] = targetType;
            ViewData["CurrentSearch"] = searchQuery;

            var query = _context.Reports
                .Include(r => r.Author)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            if (targetType.HasValue)
            {
                query = query.Where(r => r.TargetType == targetType.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var normalizedQuery = searchQuery.Trim();

                query = query.Where(r =>
                    r.Description.Contains(normalizedQuery) ||
                    r.TargetId.Contains(normalizedQuery) ||
                    (r.Author != null && (
                        r.Author.Email!.Contains(normalizedQuery) ||
                        r.Author.FirstName.Contains(normalizedQuery) ||
                        r.Author.LastName.Contains(normalizedQuery))));
            }

            var orderedQuery = query
                .OrderBy(r => r.Status != ReportStatus.New)
                .ThenBy(r => r.Status != ReportStatus.InReview)
                .ThenByDescending(r => r.CreatedAt);

            var totalItems = await orderedQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            ViewData["PageNumber"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalItems"] = totalItems;

            var reports = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new List<AdminReportListItemViewModel>();

            foreach (var report in reports)
            {
                model.Add(new AdminReportListItemViewModel
                {
                    Id = report.Id,
                    AuthorName = report.Author == null
                        ? "Неизвестный пользователь"
                        : $"{report.Author.FirstName} {report.Author.LastName}",
                    AuthorEmail = report.Author?.Email ?? "",
                    TargetType = report.TargetType,
                    TargetTypeText = GetTargetTypeText(report.TargetType),
                    TargetTitle = await GetTargetTitleAsync(report.TargetType, report.TargetId),
                    DescriptionPreview = report.Description.Length > 120
                        ? report.Description.Substring(0, 120) + "..."
                        : report.Description,
                    Status = report.Status,
                    StatusText = GetStatusText(report.Status),
                    StatusCssClass = GetStatusCssClass(report.Status),
                    CreatedAt = report.CreatedAt
                });
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var report = await _context.Reports
                .Include(r => r.Author)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
            {
                return NotFound();
            }

            var model = await BuildDetailsViewModelAsync(report);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TakeInWork(int id)
        {
            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
            {
                return NotFound();
            }

            if (IsFinalStatus(report.Status))
            {
                TempData["ErrorMessage"] = "Жалоба уже обработана. Повторная обработка недоступна.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (report.Status == ReportStatus.InReview)
            {
                TempData["SuccessMessage"] = "Жалоба уже находится в работе";
                return RedirectToAction(nameof(Details), new { id });
            }

            report.Status = ReportStatus.InReview;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Жалоба взята в работу";

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Close(ProcessReportViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Введите комментарий администратора";
                return RedirectToAction(nameof(Details), new { id = model.ReportId });
            }

            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.Id == model.ReportId);

            if (report == null)
            {
                return NotFound();
            }

            if (IsFinalStatus(report.Status))
            {
                TempData["ErrorMessage"] = "Жалоба уже обработана. Повторное закрытие недоступно.";
                return RedirectToAction(nameof(Details), new { id = report.Id });
            }

            report.Status = ReportStatus.Closed;
            report.AdminComment = model.AdminComment.Trim();
            report.ReviewedAt = DateTime.Now;

            _context.Notifications.Add(new Notification
            {
                UserId = report.AuthorId,
                Title = "Жалоба рассмотрена",
                Message = $"Ваша жалоба №{report.Id} была рассмотрена администрацией. Решение: жалоба закрыта.",
                Link = null,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Жалоба закрыта. Повторная обработка этой жалобы теперь недоступна.";

            return RedirectToAction(nameof(Details), new { id = report.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(ProcessReportViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Введите комментарий администратора";
                return RedirectToAction(nameof(Details), new { id = model.ReportId });
            }

            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.Id == model.ReportId);

            if (report == null)
            {
                return NotFound();
            }

            if (IsFinalStatus(report.Status))
            {
                TempData["ErrorMessage"] = "Жалоба уже обработана. Повторное отклонение недоступно.";
                return RedirectToAction(nameof(Details), new { id = report.Id });
            }

            report.Status = ReportStatus.Rejected;
            report.AdminComment = model.AdminComment.Trim();
            report.ReviewedAt = DateTime.Now;

            _context.Notifications.Add(new Notification
            {
                UserId = report.AuthorId,
                Title = "Жалоба отклонена",
                Message = $"Ваша жалоба №{report.Id} была рассмотрена администрацией. Решение: жалоба отклонена.",
                Link = null,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Жалоба отклонена. Повторная обработка этой жалобы теперь недоступна.";

            return RedirectToAction(nameof(Details), new { id = report.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockTargetUser(int id)
        {
            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.Id == id && r.TargetType == ReportTargetType.User);

            if (report == null)
            {
                return NotFound();
            }

            if (IsFinalStatus(report.Status))
            {
                TempData["ErrorMessage"] = "Жалоба уже обработана. Действия по объекту недоступны.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var targetUser = await _userManager.FindByIdAsync(report.TargetId);

            if (targetUser == null)
            {
                return NotFound();
            }

            if (targetUser.IsBlocked)
            {
                TempData["SuccessMessage"] = "Пользователь уже заблокирован";
                return RedirectToAction(nameof(Details), new { id });
            }

            targetUser.IsBlocked = true;

            await _userManager.UpdateAsync(targetUser);

            _context.Notifications.Add(new Notification
            {
                UserId = targetUser.Id,
                Title = "Учётная запись заблокирована",
                Message = "Ваша учётная запись была заблокирована администрацией.",
                Link = null,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Пользователь заблокирован";

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockTargetUser(int id)
        {
            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.Id == id && r.TargetType == ReportTargetType.User);

            if (report == null)
            {
                return NotFound();
            }

            if (IsFinalStatus(report.Status))
            {
                TempData["ErrorMessage"] = "Жалоба уже обработана. Действия по объекту недоступны.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var targetUser = await _userManager.FindByIdAsync(report.TargetId);

            if (targetUser == null)
            {
                return NotFound();
            }

            if (!targetUser.IsBlocked)
            {
                TempData["SuccessMessage"] = "Пользователь уже разблокирован";
                return RedirectToAction(nameof(Details), new { id });
            }

            targetUser.IsBlocked = false;

            await _userManager.UpdateAsync(targetUser);

            _context.Notifications.Add(new Notification
            {
                UserId = targetUser.Id,
                Title = "Учётная запись разблокирована",
                Message = "Ваша учётная запись была разблокирована администрацией.",
                Link = null,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Пользователь разблокирован";

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelTargetEvent(int id)
        {
            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.Id == id && r.TargetType == ReportTargetType.Event);

            if (report == null)
            {
                return NotFound();
            }

            if (IsFinalStatus(report.Status))
            {
                TempData["ErrorMessage"] = "Жалоба уже обработана. Действия по объекту недоступны.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!int.TryParse(report.TargetId, out var eventId))
            {
                return NotFound();
            }

            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventEntity == null)
            {
                return NotFound();
            }

            if (eventEntity.Status == EventStatus.Cancelled)
            {
                TempData["SuccessMessage"] = "Мероприятие уже отменено";
                return RedirectToAction(nameof(Details), new { id });
            }

            eventEntity.Status = EventStatus.Cancelled;

            _context.Notifications.Add(new Notification
            {
                UserId = eventEntity.CreatorId,
                Title = "Мероприятие отменено администрацией",
                Message = $"Мероприятие «{eventEntity.Title}» было отменено администрацией.",
                Link = $"/Events/Details/{eventEntity.Id}",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Мероприятие отменено";

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HideTargetMessage(int id)
        {
            var report = await _context.Reports
                .FirstOrDefaultAsync(r => r.Id == id && r.TargetType == ReportTargetType.Message);

            if (report == null)
            {
                return NotFound();
            }

            if (IsFinalStatus(report.Status))
            {
                TempData["ErrorMessage"] = "Жалоба уже обработана. Действия по объекту недоступны.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!int.TryParse(report.TargetId, out var messageId))
            {
                return NotFound();
            }

            var message = await _context.EventMessages
                .Include(m => m.Event)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return NotFound();
            }

            if (message.IsDeleted)
            {
                TempData["SuccessMessage"] = "Сообщение уже скрыто";
                return RedirectToAction(nameof(Details), new { id });
            }

            message.IsDeleted = true;

            _context.Notifications.Add(new Notification
            {
                UserId = message.UserId,
                Title = "Сообщение удалено администрацией",
                Message = $"Ваше сообщение в чате мероприятия «{message.Event?.Title ?? "Без названия"}» было удалено администрацией.",
                Link = $"/Events/Details/{message.EventId}",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Сообщение скрыто";

            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<AdminReportDetailsViewModel> BuildDetailsViewModelAsync(Report report)
        {
            var canProcess = !IsFinalStatus(report.Status);

            var model = new AdminReportDetailsViewModel
            {
                Id = report.Id,
                AuthorId = report.AuthorId,
                AuthorName = report.Author == null
                    ? "Неизвестный пользователь"
                    : $"{report.Author.FirstName} {report.Author.LastName}",
                AuthorEmail = report.Author?.Email ?? "",
                TargetType = report.TargetType,
                TargetTypeText = GetTargetTypeText(report.TargetType),
                TargetId = report.TargetId,
                TargetTitle = await GetTargetTitleAsync(report.TargetType, report.TargetId),
                TargetLink = await GetTargetLinkAsync(report.TargetType, report.TargetId),
                Description = report.Description,
                Status = report.Status,
                StatusText = GetStatusText(report.Status),
                StatusCssClass = GetStatusCssClass(report.Status),
                CreatedAt = report.CreatedAt,
                ReviewedAt = report.ReviewedAt,
                AdminComment = report.AdminComment,
                CanProcess = canProcess,
                CanTakeInWork = report.Status == ReportStatus.New
            };

            if (report.TargetType == ReportTargetType.User)
            {
                var user = await _userManager.FindByIdAsync(report.TargetId);

                model.CanBlockUser = canProcess && user != null;
                model.TargetUserIsBlocked = user?.IsBlocked ?? false;
            }

            if (report.TargetType == ReportTargetType.Event &&
                int.TryParse(report.TargetId, out var eventId))
            {
                var eventEntity = await _context.Events
                    .FirstOrDefaultAsync(e => e.Id == eventId);

                model.CanCancelEvent = canProcess &&
                    eventEntity != null &&
                    eventEntity.Status != EventStatus.Cancelled;

                model.TargetEventStatus = eventEntity?.Status;
            }

            if (report.TargetType == ReportTargetType.Message &&
                int.TryParse(report.TargetId, out var messageId))
            {
                var message = await _context.EventMessages
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                model.CanHideMessage = canProcess &&
                    message != null &&
                    !message.IsDeleted;

                model.TargetMessageIsDeleted = message?.IsDeleted ?? false;
            }

            return model;
        }

        private async Task<string> GetTargetTitleAsync(ReportTargetType targetType, string targetId)
        {
            switch (targetType)
            {
                case ReportTargetType.User:
                    var user = await _userManager.FindByIdAsync(targetId);
                    return user == null
                        ? "Пользователь не найден"
                        : $"{user.FirstName} {user.LastName}";

                case ReportTargetType.Event:
                    if (int.TryParse(targetId, out var eventId))
                    {
                        var eventEntity = await _context.Events.FirstOrDefaultAsync(e => e.Id == eventId);
                        return eventEntity?.Title ?? "Мероприятие не найдено";
                    }

                    return "Мероприятие не найдено";

                case ReportTargetType.Message:
                    if (int.TryParse(targetId, out var messageId))
                    {
                        var message = await _context.EventMessages
                            .Include(m => m.Event)
                            .FirstOrDefaultAsync(m => m.Id == messageId);

                        return message == null
                            ? "Сообщение не найдено"
                            : $"Сообщение в чате мероприятия «{message.Event?.Title ?? "Без названия"}»";
                    }

                    return "Сообщение не найдено";

                default:
                    return "Неизвестный объект";
            }
        }

        private async Task<string?> GetTargetLinkAsync(ReportTargetType targetType, string targetId)
        {
            switch (targetType)
            {
                case ReportTargetType.User:
                    var user = await _userManager.FindByIdAsync(targetId);
                    return user == null ? null : $"/Profile/User/{user.Id}";

                case ReportTargetType.Event:
                    return int.TryParse(targetId, out var eventId)
                        ? $"/Events/Details/{eventId}"
                        : null;

                case ReportTargetType.Message:
                    if (int.TryParse(targetId, out var messageId))
                    {
                        var message = await _context.EventMessages
                            .FirstOrDefaultAsync(m => m.Id == messageId);

                        return message == null
                            ? null
                            : $"/Events/Details/{message.EventId}";
                    }

                    return null;

                default:
                    return null;
            }
        }

        private static string GetTargetTypeText(ReportTargetType targetType)
        {
            return targetType switch
            {
                ReportTargetType.User => "Профиль пользователя",
                ReportTargetType.Event => "Мероприятие",
                ReportTargetType.Message => "Сообщение чата",
                _ => "Неизвестный объект"
            };
        }

        private static string GetStatusText(ReportStatus status)
        {
            return status switch
            {
                ReportStatus.New => "Новая",
                ReportStatus.InReview => "В работе",
                ReportStatus.Closed => "Закрыта",
                ReportStatus.Rejected => "Отклонена",
                _ => "Неизвестно"
            };
        }

        private static string GetStatusCssClass(ReportStatus status)
        {
            return status switch
            {
                ReportStatus.New => "text-bg-danger",
                ReportStatus.InReview => "text-bg-warning",
                ReportStatus.Closed => "text-bg-success",
                ReportStatus.Rejected => "text-bg-secondary",
                _ => "text-bg-light"
            };
        }

        private static bool IsFinalStatus(ReportStatus status)
        {
            return status == ReportStatus.Closed ||
                   status == ReportStatus.Rejected;
        }
    }
}