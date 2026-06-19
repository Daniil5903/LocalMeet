using System.ComponentModel.DataAnnotations;

namespace LocalMeet.Models.ViewModels.Admin
{
    public class CategoryFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название категории")]
        [StringLength(100, ErrorMessage = "Название категории не должно превышать 100 символов")]
        [Display(Name = "Название категории")]
        public string Name { get; set; } = string.Empty;
    }
}