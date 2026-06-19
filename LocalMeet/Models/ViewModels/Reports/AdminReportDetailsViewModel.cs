using LocalMeet.Models.Enums;

namespace LocalMeet.Models.ViewModels.Reports
{
    public class AdminReportDetailsViewModel
    {
        public int Id { get; set; }

        public string AuthorId { get; set; } = string.Empty;

        public string AuthorName { get; set; } = string.Empty;

        public string AuthorEmail { get; set; } = string.Empty;

        public ReportTargetType TargetType { get; set; }

        public string TargetTypeText { get; set; } = string.Empty;

        public string TargetId { get; set; } = string.Empty;

        public string TargetTitle { get; set; } = string.Empty;

        public string? TargetLink { get; set; }

        public string Description { get; set; } = string.Empty;

        public ReportStatus Status { get; set; }

        public string StatusText { get; set; } = string.Empty;

        public string StatusCssClass { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public string? AdminComment { get; set; }

        public bool CanBlockUser { get; set; }

        public bool TargetUserIsBlocked { get; set; }

        public bool CanCancelEvent { get; set; }

        public EventStatus? TargetEventStatus { get; set; }

        public bool CanHideMessage { get; set; }

        public bool TargetMessageIsDeleted { get; set; }
    }
}