using System.ComponentModel.DataAnnotations;

namespace LocalMeet.Models.ViewModels.Reports
{
    public class ProcessReportViewModel
    {
        public int ReportId { get; set; }

        [Required(ErrorMessage = "Введите комментарий администратора")]
        [StringLength(1000, ErrorMessage = "Комментарий не должен превышать 1000 символов")]
        [Display(Name = "Комментарий администратора")]
        public string AdminComment { get; set; } = string.Empty;
    }
}