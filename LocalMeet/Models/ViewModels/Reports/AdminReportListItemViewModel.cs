using LocalMeet.Models.Enums;

namespace LocalMeet.Models.ViewModels.Reports
{
    public class AdminReportListItemViewModel
    {
        public int Id { get; set; }

        public string AuthorName { get; set; } = string.Empty;

        public string AuthorEmail { get; set; } = string.Empty;

        public ReportTargetType TargetType { get; set; }

        public string TargetTypeText { get; set; } = string.Empty;

        public string TargetTitle { get; set; } = string.Empty;

        public string DescriptionPreview { get; set; } = string.Empty;

        public ReportStatus Status { get; set; }

        public string StatusText { get; set; } = string.Empty;

        public string StatusCssClass { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}