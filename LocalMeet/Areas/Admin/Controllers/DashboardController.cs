using LocalMeet.Data;
using LocalMeet.Models.Enums;
using LocalMeet.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalMeet.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AppRole.Admin)]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var recentEvents = await _context.Events
                .Include(e => e.Creator)
                .OrderByDescending(e => e.CreatedAt)
                .Take(5)
                .ToListAsync();

            var recentReports = await _context.Reports
                .Include(r => r.Author)
                .OrderBy(r => r.Status != ReportStatus.New)
                .ThenByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();

            var model = new DashboardViewModel
            {
                UsersCount = await _context.Users.CountAsync(),
                BlockedUsersCount = await _context.Users.CountAsync(u => u.IsBlocked),
                EventsCount = await _context.Events.CountAsync(),
                PendingEventsCount = await _context.Events.CountAsync(e => e.Status == EventStatus.Pending),
                ApprovedEventsCount = await _context.Events.CountAsync(e => e.Status == EventStatus.Approved),
                ReportsCount = await _context.Reports.CountAsync(),
                NewReportsCount = await _context.Reports.CountAsync(r => r.Status == ReportStatus.New),
                CategoriesCount = await _context.Categories.CountAsync(),
                ParticipationsCount = await _context.Participations.CountAsync(),
                UnreadNotificationsCount = await _context.Notifications.CountAsync(n => !n.IsRead),
                RecentEvents = recentEvents
                    .Select(e => new DashboardEventItemViewModel
                    {
                        Id = e.Id,
                        Title = e.Title,
                        CreatorName = e.Creator == null
                            ? "Неизвестный пользователь"
                            : e.Creator.FirstName + " " + e.Creator.LastName,
                        CreatedAt = e.CreatedAt,
                        Status = e.Status,
                        StatusText = GetEventStatusText(e.Status),
                        StatusCssClass = GetEventStatusCssClass(e.Status)
                    })
                    .ToList(),
                RecentReports = recentReports
                    .Select(r => new DashboardReportItemViewModel
                    {
                        Id = r.Id,
                        AuthorName = r.Author == null
                            ? "Неизвестный пользователь"
                            : r.Author.FirstName + " " + r.Author.LastName,
                        TargetType = r.TargetType,
                        TargetTypeText = GetTargetTypeText(r.TargetType),
                        CreatedAt = r.CreatedAt,
                        Status = r.Status,
                        StatusText = GetReportStatusText(r.Status),
                        StatusCssClass = GetReportStatusCssClass(r.Status)
                    })
                    .ToList()
            };

            return View(model);
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

        private static string GetReportStatusText(ReportStatus status)
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

        private static string GetReportStatusCssClass(ReportStatus status)
        {
            return status switch
            {
                ReportStatus.New => "bg-primary",
                ReportStatus.InReview => "bg-warning text-dark",
                ReportStatus.Closed => "bg-success",
                ReportStatus.Rejected => "bg-secondary",
                _ => "bg-light text-dark"
            };
        }

        private static string GetTargetTypeText(ReportTargetType targetType)
        {
            return targetType switch
            {
                ReportTargetType.User => "Профиль",
                ReportTargetType.Event => "Мероприятие",
                ReportTargetType.Message => "Сообщение",
                _ => "Объект"
            };
        }
    }
}