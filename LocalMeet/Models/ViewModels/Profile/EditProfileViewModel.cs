using System.ComponentModel.DataAnnotations;

namespace LocalMeet.Models.ViewModels.Profile
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Введите имя")]
        [StringLength(50, ErrorMessage = "Имя не должно превышать 50 символов")]
        [Display(Name = "Имя")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите фамилию")]
        [StringLength(50, ErrorMessage = "Фамилия не должна превышать 50 символов")]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Описание не должно превышать 500 символов")]
        [Display(Name = "О себе")]
        public string? About { get; set; }

        [Display(Name = "Закрытый профиль")]
        public bool IsPrivateProfile { get; set; }

        public string? CurrentAvatarPath { get; set; }

        [Display(Name = "Аватар")]
        public IFormFile? AvatarFile { get; set; }
    }
}