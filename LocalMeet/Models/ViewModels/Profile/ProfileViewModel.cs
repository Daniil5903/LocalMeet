namespace LocalMeet.Models.ViewModels.Profile
{
    public class ProfileViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {LastName}".Trim();

        public string Email { get; set; } = string.Empty;

        public string? AvatarPath { get; set; }

        public string? About { get; set; }

        public bool IsPrivateProfile { get; set; }

        public bool IsBlocked { get; set; }

        public bool IsProfileUserAdmin { get; set; }

        public DateTime RegistrationDate { get; set; }

        public DateTime? LastVisit { get; set; }

        public bool IsOwnProfile { get; set; }

        public bool IsCurrentUserAdmin { get; set; }

        public bool CanViewPrivateInfo { get; set; }

        public bool CanEditProfile { get; set; }

        public bool CanReportUser { get; set; }

        public bool CanAdminBlockUser { get; set; }

        public bool CanAdminUnblockUser { get; set; }

        public bool CanOpenAdminCard { get; set; }

        public int CreatedEventsCount { get; set; }

        public int ParticipatedEventsCount { get; set; }

        public int FavoriteEventsCount { get; set; }

        public int MessagesCount { get; set; }

        public List<ProfileEventItemViewModel> RecentCreatedEvents { get; set; } = new();

        public List<ProfileEventItemViewModel> RecentParticipations { get; set; } = new();
    }

    public class ProfileEventItemViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public string StatusText { get; set; } = string.Empty;

        public string StatusCssClass { get; set; } = string.Empty;
    }
}