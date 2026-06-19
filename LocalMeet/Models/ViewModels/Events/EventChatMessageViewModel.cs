namespace LocalMeet.Models.ViewModels.Events
{
    public class EventChatMessageViewModel
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string AuthorName { get; set; } = string.Empty;

        public string? AuthorAvatarPath { get; set; }

        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public bool IsDeleted { get; set; }

        public bool CanReport { get; set; }
    }
}