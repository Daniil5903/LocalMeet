namespace LocalMeet.Models.ViewModels.Admin
{
    public class UserManagementIndexViewModel
    {
        public string? SearchQuery { get; set; }

        public string? RoleFilter { get; set; }

        public string? StatusFilter { get; set; }

        public string? SortOrder { get; set; }

        public int TotalUsersCount { get; set; }

        public int ActiveUsersCount { get; set; }

        public int BlockedUsersCount { get; set; }

        public int AdminsCount { get; set; }

        public List<UserManagementListItemViewModel> Users { get; set; } = new();

        public int PageNumber { get; set; }

        public int TotalPages { get; set; }

        public int TotalItems { get; set; }

        public bool HasPreviousPage => PageNumber > 1;

        public bool HasNextPage => PageNumber < TotalPages;
    }

    public class UserManagementListItemViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public bool IsBlocked { get; set; }

        public bool IsAdmin { get; set; }

        public bool IsPrivateProfile { get; set; }

        public DateTime RegistrationDate { get; set; }

        public DateTime? LastVisit { get; set; }

        public int CreatedEventsCount { get; set; }

        public int ParticipationsCount { get; set; }
    }

    public class UserManagementDetailsViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? AvatarPath { get; set; }

        public string? About { get; set; }

        public bool IsBlocked { get; set; }

        public bool IsAdmin { get; set; }

        public bool IsPrivateProfile { get; set; }

        public DateTime RegistrationDate { get; set; }

        public DateTime? LastVisit { get; set; }

        public int CreatedEventsCount { get; set; }

        public int ParticipationsCount { get; set; }

        public int FavoriteEventsCount { get; set; }

        public int ReportsCreatedCount { get; set; }

        public int MessagesCount { get; set; }

        public List<UserManagementEventItemViewModel> RecentCreatedEvents { get; set; } = new();

        public List<UserManagementEventItemViewModel> RecentParticipations { get; set; } = new();
    }

    public class UserManagementEventItemViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public string StatusText { get; set; } = string.Empty;

        public string StatusCssClass { get; set; } = string.Empty;
    }
}