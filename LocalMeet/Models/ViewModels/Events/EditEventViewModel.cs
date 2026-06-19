using LocalMeet.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace LocalMeet.Models.ViewModels.Events
{
    public class EditEventViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название мероприятия")]
        [StringLength(150, ErrorMessage = "Название не должно превышать 150 символов")]
        [Display(Name = "Название")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите описание мероприятия")]
        [Display(Name = "Описание")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Укажите адрес проведения")]
        [StringLength(255, ErrorMessage = "Адрес не должен превышать 255 символов")]
        [Display(Name = "Адрес")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Выберите точку на карте")]
        public decimal? Latitude { get; set; }

        [Required(ErrorMessage = "Выберите точку на карте")]
        public decimal? Longitude { get; set; }

        [Required(ErrorMessage = "Укажите дату и время проведения")]
        [Display(Name = "Дата и время проведения")]
        public DateTime EventDate { get; set; }

        [Required(ErrorMessage = "Укажите максимальное количество участников")]
        [Range(1, 10000, ErrorMessage = "Количество участников должно быть больше 0")]
        [Display(Name = "Максимальное количество участников")]
        public int MaxParticipants { get; set; }

        [Required(ErrorMessage = "Выберите категорию")]
        [Display(Name = "Категория")]
        public int CategoryId { get; set; }

        public EventStatus Status { get; set; }

        public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    }
}