namespace LocalMeet.Models.ViewModels.Events
{
    public class EventParticipantViewModel
    {
        public string UserId { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? AvatarPath { get; set; }

        public DateTime JoinedAt { get; set; }
    }
}