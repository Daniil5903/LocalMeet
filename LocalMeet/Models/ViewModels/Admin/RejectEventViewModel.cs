using System.ComponentModel.DataAnnotations;

namespace LocalMeet.Models.ViewModels.Admin
{
    public class RejectEventViewModel
    {
        public int EventId { get; set; }

        public string EventTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Укажите причину отклонения")]
        [StringLength(500, ErrorMessage = "Причина отклонения не должна превышать 500 символов")]
        [Display(Name = "Причина отклонения")]
        public string RejectReason { get; set; } = string.Empty;
    }
}