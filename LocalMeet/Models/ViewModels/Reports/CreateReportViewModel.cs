using LocalMeet.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace LocalMeet.Models.ViewModels.Reports
{
    public class CreateReportViewModel
    {
        public ReportTargetType TargetType { get; set; }

        public string TargetId { get; set; } = string.Empty;

        public string TargetTitle { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }

        [Required(ErrorMessage = "Опишите причину жалобы")]
        [StringLength(1000, ErrorMessage = "Описание жалобы не должно превышать 1000 символов")]
        [Display(Name = "Описание нарушения")]
        public string Description { get; set; } = string.Empty;
    }
}