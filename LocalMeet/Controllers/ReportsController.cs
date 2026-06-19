using LocalMeet.Data;
using LocalMeet.Models.Entities;
using LocalMeet.Models.Enums;
using LocalMeet.Models.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalMeet.Controllers
{
    [Authorize]
    [Route("Reports")]
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

        [HttpGet("CreateForUser/{userId}")]
        public async Task<IActionResult> CreateForUser(string userId, string? returnUrl = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (currentUser.IsBlocked)
            {
                TempData["ErrorMessage"] = "Заблокированный пользователь не может отправлять жалобы";
                return RedirectToAction("Index", "Home");
            }

            if (currentUser.Id == userId)
            {
                TempData["ErrorMessage"] = "Нельзя отправить жалобу на собственный профиль";
                return RedirectToAction("Index", "Profile");
            }

            var targetUser = await _userManager.FindByIdAsync(userId);

            if (targetUser == null)
            {
                return NotFound();
            }

            var model = new CreateReportViewModel
            {
                TargetType = ReportTargetType.User,
                TargetId = targetUser.Id,
                TargetTitle = $"{targetUser.FirstName} {targetUser.LastName}",
                ReturnUrl = returnUrl
            };

            return View("Create", model);
        }

        [HttpGet("CreateForEvent/{eventId:int}")]
        public async Task<IActionResult> CreateForEvent(int eventId, string? returnUrl = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (currentUser.IsBlocked)
            {
                TempData["ErrorMessage"] = "Заблокированный пользователь не может отправлять жалобы";
                return RedirectToAction("Details", "Events", new { id = eventId });
            }

            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventEntity == null)
            {
                return NotFound();
            }

            var model = new CreateReportViewModel
            {
                TargetType = ReportTargetType.Event,
                TargetId = eventEntity.Id.ToString(),
                TargetTitle = eventEntity.Title,
                ReturnUrl = returnUrl
            };

            return View("Create", model);
        }

        [HttpGet("CreateForMessage/{messageId:int}")]
        public async Task<IActionResult> CreateForMessage(int messageId, string? returnUrl = null)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (currentUser.IsBlocked)
            {
                TempData["ErrorMessage"] = "Заблокированный пользователь не может отправлять жалобы";
                return RedirectToAction("Index", "Home");
            }

            var message = await _context.EventMessages
                .Include(m => m.Event)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return NotFound();
            }

            var model = new CreateReportViewModel
            {
                TargetType = ReportTargetType.Message,
                TargetId = message.Id.ToString(),
                TargetTitle = $"Сообщение в чате мероприятия «{message.Event?.Title ?? "Без названия"}»",
                ReturnUrl = returnUrl ?? $"/Events/Details/{message.EventId}"
            };

            return View("Create", model);
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateReportViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (currentUser.IsBlocked)
            {
                TempData["ErrorMessage"] = "Заблокированный пользователь не может отправлять жалобы";
                return RedirectToAction("Index", "Home");
            }

            var targetExists = await TargetExistsAsync(model.TargetType, model.TargetId);

            if (!targetExists)
            {
                ModelState.AddModelError(string.Empty, "Объект жалобы не найден");
            }

            if (!ModelState.IsValid)
            {
                model.TargetTitle = await GetTargetTitleAsync(model.TargetType, model.TargetId);
                return View(model);
            }

            var report = new Report
            {
                AuthorId = currentUser.Id,
                TargetType = model.TargetType,
                TargetId = model.TargetId,
                Description = model.Description.Trim(),
                Status = ReportStatus.New,
                CreatedAt = DateTime.Now
            };

            _context.Reports.Add(report);

            var admins = await _userManager.GetUsersInRoleAsync(AppRole.Admin);

            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Title = "Новая жалоба",
                    Message = $"{currentUser.FirstName} {currentUser.LastName} отправил жалобу: {GetTargetTypeText(model.TargetType)}.",
                    Link = "/Admin/Reports",
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Жалоба успешно отправлена администрации";

            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        private async Task<bool> TargetExistsAsync(ReportTargetType targetType, string targetId)
        {
            return targetType switch
            {
                ReportTargetType.User => await _userManager.FindByIdAsync(targetId) != null,

                ReportTargetType.Event => int.TryParse(targetId, out var eventId) &&
                    await _context.Events.AnyAsync(e => e.Id == eventId),

                ReportTargetType.Message => int.TryParse(targetId, out var messageId) &&
                    await _context.EventMessages.AnyAsync(m => m.Id == messageId),

                _ => false
            };
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

        private static string GetTargetTypeText(ReportTargetType targetType)
        {
            return targetType switch
            {
                ReportTargetType.User => "профиль пользователя",
                ReportTargetType.Event => "мероприятие",
                ReportTargetType.Message => "сообщение чата",
                _ => "объект"
            };
        }
    }
}