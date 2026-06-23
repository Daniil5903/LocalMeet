using LocalMeet.Data;
using LocalMeet.Models.Entities;
using LocalMeet.Models.Enums;
using LocalMeet.Models.ViewModels.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalMeet.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private const long MaxAvatarSize =
            2 * 1024 * 1024;

        private static readonly string[]
            AllowedAvatarExtensions =
            {
                ".jpg",
                ".jpeg",
                ".png"
            };

        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public ProfileController(
            UserManager<User> userManager,
            IWebHostEnvironment environment,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _environment = environment;
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
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

            var model =
                await BuildProfileViewModelAsync(
                    currentUser,
                    currentUser,
                    isAdmin);

            return View("Details", model);
        }

        [HttpGet("/Profile/User/{id}")]
        public async Task<IActionResult> ViewUser(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var profileUser =
                await _userManager.FindByIdAsync(id);

            if (profileUser == null)
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

            var model =
                await BuildProfileViewModelAsync(
                    profileUser,
                    currentUser,
                    isAdmin);

            return View("Details", model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var model = new EditProfileViewModel
            {
                FirstName = currentUser.FirstName,
                LastName = currentUser.LastName,
                About = currentUser.About,
                IsPrivateProfile =
                    currentUser.IsPrivateProfile,
                CurrentAvatarPath =
                    currentUser.AvatarPath
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            EditProfileViewModel model)
        {
            var currentUser =
                await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            model.CurrentAvatarPath =
                currentUser.AvatarPath;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            currentUser.FirstName =
                model.FirstName.Trim();

            currentUser.LastName =
                model.LastName.Trim();

            currentUser.About =
                model.About?.Trim();

            currentUser.IsPrivateProfile =
                model.IsPrivateProfile;

            if (model.AvatarFile != null)
            {
                var avatarResult =
                    await SaveAvatarAsync(
                        model.AvatarFile);

                if (!avatarResult.Success)
                {
                    ModelState.AddModelError(
                        nameof(model.AvatarFile),
                        avatarResult.ErrorMessage!);

                    return View(model);
                }

                DeleteOldAvatar(
                    currentUser.AvatarPath);

                currentUser.AvatarPath =
                    avatarResult.FilePath;
            }

            var result =
                await _userManager.UpdateAsync(
                    currentUser);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(model);
            }

            TempData["SuccessMessage"] =
                "Профиль успешно обновлён";

            return RedirectToAction(
                nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = AppRole.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(
            string id,
            string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var targetUser =
                await _userManager.FindByIdAsync(id);

            if (targetUser == null)
            {
                return NotFound();
            }

            var currentUserId =
                _userManager.GetUserId(User);

            if (targetUser.Id == currentUserId)
            {
                TempData["ErrorMessage"] =
                    "Нельзя заблокировать собственную учётную запись";

                return RedirectAfterUserAction(
                    targetUser.Id,
                    returnUrl);
            }

            var targetUserIsAdmin =
                await _userManager.IsInRoleAsync(
                    targetUser,
                    AppRole.Admin);

            if (targetUserIsAdmin)
            {
                TempData["ErrorMessage"] =
                    "Нельзя заблокировать администратора";

                return RedirectAfterUserAction(
                    targetUser.Id,
                    returnUrl);
            }

            if (!targetUser.IsBlocked)
            {
                targetUser.IsBlocked = true;

                await _userManager.UpdateAsync(
                    targetUser);

                _context.Notifications.Add(
                    new Notification
                    {
                        UserId = targetUser.Id,
                        Title = "Учётная запись заблокирована",
                        Message = "Ваша учётная запись была заблокирована администратором.",
                        Link = null,
                        CreatedAt = DateTime.Now
                    });

                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] =
                "Пользователь заблокирован";

            return RedirectAfterUserAction(
                targetUser.Id,
                returnUrl);
        }

        [HttpPost]
        [Authorize(Roles = AppRole.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockUser(
            string id,
            string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var targetUser =
                await _userManager.FindByIdAsync(id);

            if (targetUser == null)
            {
                return NotFound();
            }

            if (targetUser.IsBlocked)
            {
                targetUser.IsBlocked = false;

                await _userManager.UpdateAsync(
                    targetUser);

                _context.Notifications.Add(
                    new Notification
                    {
                        UserId = targetUser.Id,
                        Title = "Учётная запись разблокирована",
                        Message = "Ваша учётная запись была разблокирована администратором.",
                        Link = null,
                        CreatedAt = DateTime.Now
                    });

                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] =
                "Пользователь разблокирован";

            return RedirectAfterUserAction(
                targetUser.Id,
                returnUrl);
        }

        private async Task<ProfileViewModel>
            BuildProfileViewModelAsync(
                User profileUser,
                User currentUser,
                bool isAdmin)
        {
            var isOwnProfile =
                profileUser.Id == currentUser.Id;

            var profileUserIsAdmin =
                await _userManager.IsInRoleAsync(
                    profileUser,
                    AppRole.Admin);

            var canViewPrivateInfo =
                isOwnProfile ||
                isAdmin ||
                !profileUser.IsPrivateProfile;

            var recentCreatedEvents =
                await _context.Events
                    .Where(eventEntity =>
                        eventEntity.CreatorId == profileUser.Id)
                    .OrderByDescending(eventEntity =>
                        eventEntity.CreatedAt)
                    .Take(5)
                    .ToListAsync();

            var recentParticipations =
                await _context.Participations
                    .Where(participation =>
                        participation.UserId == profileUser.Id)
                    .Include(participation =>
                        participation.Event)
                    .OrderByDescending(participation =>
                        participation.CreatedAt)
                    .Take(5)
                    .ToListAsync();

            return new ProfileViewModel
            {
                Id = profileUser.Id,
                FirstName = profileUser.FirstName,
                LastName = profileUser.LastName,
                Email =
                    profileUser.Email ??
                    string.Empty,
                AvatarPath =
                    profileUser.AvatarPath,
                About = profileUser.About,
                IsPrivateProfile =
                    profileUser.IsPrivateProfile,
                IsBlocked =
                    profileUser.IsBlocked,
                IsProfileUserAdmin =
                    profileUserIsAdmin,
                RegistrationDate =
                    profileUser.RegistrationDate,
                LastVisit =
                    profileUser.LastVisit,
                IsOwnProfile =
                    isOwnProfile,
                IsCurrentUserAdmin =
                    isAdmin,
                CanViewPrivateInfo =
                    canViewPrivateInfo,
                CanEditProfile =
                    isOwnProfile,
                CanReportUser =
                    !isOwnProfile &&
                    !isAdmin,
                CanAdminBlockUser =
                    isAdmin &&
                    !isOwnProfile &&
                    !profileUserIsAdmin &&
                    !profileUser.IsBlocked,
                CanAdminUnblockUser =
                    isAdmin &&
                    !isOwnProfile &&
                    !profileUserIsAdmin &&
                    profileUser.IsBlocked,
                CanOpenAdminCard =
                    isAdmin,
                CreatedEventsCount =
                    await _context.Events.CountAsync(
                        eventEntity =>
                            eventEntity.CreatorId ==
                            profileUser.Id),
                ParticipatedEventsCount =
                    await _context.Participations
                        .CountAsync(
                            participation =>
                                participation.UserId ==
                                profileUser.Id),
                FavoriteEventsCount =
                    await _context.FavoriteEvents
                        .CountAsync(
                            favorite =>
                                favorite.UserId ==
                                profileUser.Id),
                MessagesCount =
                    await _context.EventMessages
                        .CountAsync(
                            message =>
                                message.UserId ==
                                profileUser.Id &&
                                !message.IsDeleted),
                RecentCreatedEvents =
                    recentCreatedEvents
                        .Select(eventEntity =>
                            new ProfileEventItemViewModel
                            {
                                Id = eventEntity.Id,
                                Title = eventEntity.Title,
                                EventDate = eventEntity.EventDate,
                                StatusText =
                                    GetEventStatusText(
                                        eventEntity.Status),
                                StatusCssClass =
                                    GetEventStatusCssClass(
                                        eventEntity.Status)
                            })
                        .ToList(),
                RecentParticipations =
                    recentParticipations
                        .Select(participation =>
                            new ProfileEventItemViewModel
                            {
                                Id = participation.EventId,
                                Title = participation.Event == null
                                    ? "Мероприятие не найдено"
                                    : participation.Event.Title,
                                EventDate = participation.Event == null
                                    ? DateTime.MinValue
                                    : participation.Event.EventDate,
                                StatusText = participation.Event == null
                                    ? "Неизвестно"
                                    : GetEventStatusText(
                                        participation.Event.Status),
                                StatusCssClass = participation.Event == null
                                    ? "text-bg-light"
                                    : GetEventStatusCssClass(
                                        participation.Event.Status)
                            })
                        .ToList()
            };
        }

        private IActionResult RedirectAfterUserAction(
            string targetUserId,
            string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(
                nameof(ViewUser),
                new
                {
                    id = targetUserId
                });
        }

        private static string GetEventStatusText(
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

        private static string GetEventStatusCssClass(
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

        private async Task<AvatarSaveResult>
            SaveAvatarAsync(IFormFile avatarFile)
        {
            if (avatarFile.Length == 0)
            {
                return AvatarSaveResult.Fail(
                    "Файл пустой");
            }

            if (avatarFile.Length > MaxAvatarSize)
            {
                return AvatarSaveResult.Fail(
                    "Размер файла не должен превышать 2 МБ");
            }

            var extension = Path
                .GetExtension(avatarFile.FileName)
                .ToLowerInvariant();

            if (!AllowedAvatarExtensions.Contains(
                extension))
            {
                return AvatarSaveResult.Fail(
                    "Допустимые форматы аватара: " +
                    "jpg, jpeg, png");
            }

            var uploadsFolder =
                GetAvatarStoragePath();

            Directory.CreateDirectory(
                uploadsFolder);

            var fileName =
                $"{Guid.NewGuid()}{extension}";

            var fullPath =
                Path.Combine(
                    uploadsFolder,
                    fileName);

            await using var fileStream =
                new FileStream(
                    fullPath,
                    FileMode.Create);

            await avatarFile.CopyToAsync(
                fileStream);

            var relativePath =
                $"/uploads/avatars/{fileName}";

            return AvatarSaveResult.Ok(
                relativePath);
        }

        private void DeleteOldAvatar(
            string? avatarPath)
        {
            if (string.IsNullOrWhiteSpace(
                avatarPath))
            {
                return;
            }

            if (!avatarPath.StartsWith(
                "/uploads/avatars/",
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var fileName =
                Path.GetFileName(avatarPath);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            var fullPath =
                Path.Combine(
                    GetAvatarStoragePath(),
                    fileName);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        private string GetAvatarStoragePath()
        {
            var dataPath =
                _configuration["Storage:DataPath"];

            if (string.IsNullOrWhiteSpace(dataPath))
            {
                return Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "avatars");
            }

            var resolvedDataPath =
                Path.IsPathRooted(dataPath)
                    ? dataPath
                    : Path.GetFullPath(
                        dataPath,
                        _environment.ContentRootPath);

            return Path.Combine(
                resolvedDataPath,
                "avatars");
        }

        private class AvatarSaveResult
        {
            public bool Success { get; private set; }

            public string? FilePath
            {
                get;
                private set;
            }

            public string? ErrorMessage
            {
                get;
                private set;
            }

            public static AvatarSaveResult Ok(
                string filePath)
            {
                return new AvatarSaveResult
                {
                    Success = true,
                    FilePath = filePath
                };
            }

            public static AvatarSaveResult Fail(
                string errorMessage)
            {
                return new AvatarSaveResult
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }
        }
    }
}