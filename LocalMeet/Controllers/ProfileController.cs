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
                "Профиль успешно обновлен";

            return RedirectToAction(
                nameof(Index));
        }

        private async Task<ProfileViewModel>
            BuildProfileViewModelAsync(
                User profileUser,
                User currentUser,
                bool isAdmin)
        {
            var isOwnProfile =
                profileUser.Id == currentUser.Id;

            var canViewPrivateInfo =
                isOwnProfile ||
                isAdmin ||
                !profileUser.IsPrivateProfile;

            var createdEventsCount =
                await _context.Events.CountAsync(
                    eventEntity =>
                        eventEntity.CreatorId ==
                        profileUser.Id);

            var participatedEventsCount =
                await _context.Participations
                    .CountAsync(
                        participation =>
                            participation.UserId ==
                            profileUser.Id);

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
                RegistrationDate =
                    profileUser.RegistrationDate,
                LastVisit =
                    profileUser.LastVisit,
                IsOwnProfile =
                    isOwnProfile,
                CanViewPrivateInfo =
                    canViewPrivateInfo,
                CreatedEventsCount =
                    createdEventsCount,
                ParticipatedEventsCount =
                    participatedEventsCount
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