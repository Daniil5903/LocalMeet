namespace LocalMeet.Models.ViewModels.Profile
{
    public class ProfileViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? AvatarPath { get; set; }

        public string? About { get; set; }

        public bool IsPrivateProfile { get; set; }

        public bool IsBlocked { get; set; }

        public DateTime RegistrationDate { get; set; }

        public DateTime? LastVisit { get; set; }

        public bool IsOwnProfile { get; set; }

        public bool CanViewPrivateInfo { get; set; }

        public int CreatedEventsCount { get; set; }

        public int ParticipatedEventsCount { get; set; }
    }
}