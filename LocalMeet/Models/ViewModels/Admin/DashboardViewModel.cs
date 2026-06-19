using LocalMeet.Models.Enums;

namespace LocalMeet.Models.ViewModels.Admin
{
    public class DashboardViewModel
    {
        public int UsersCount { get; set; }

        public int BlockedUsersCount { get; set; }

        public int EventsCount { get; set; }

        public int PendingEventsCount { get; set; }

        public int ApprovedEventsCount { get; set; }

        public int ReportsCount { get; set; }

        public int NewReportsCount { get; set; }

        public int CategoriesCount { get; set; }

        public int ParticipationsCount { get; set; }

        public int UnreadNotificationsCount { get; set; }

        public List<DashboardEventItemViewModel> RecentEvents { get; set; } = new();

        public List<DashboardReportItemViewModel> RecentReports { get; set; } = new();
    }

    public class DashboardEventItemViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string CreatorName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public EventStatus Status { get; set; }

        public string StatusText { get; set; } = string.Empty;

        public string StatusCssClass { get; set; } = string.Empty;
    }

    public class DashboardReportItemViewModel
    {
        public int Id { get; set; }

        public string AuthorName { get; set; } = string.Empty;

        public ReportTargetType TargetType { get; set; }

        public string TargetTypeText { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public ReportStatus Status { get; set; }

        public string StatusText { get; set; } = string.Empty;

        public string StatusCssClass { get; set; } = string.Empty;
    }
}