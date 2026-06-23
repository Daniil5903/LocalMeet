using LocalMeet.Models.Enums;

namespace LocalMeet.Models.ViewModels.Events
{
    public class EventListItemViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string DescriptionPreview { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public DateTime CreatedAt { get; set; }

        public int MaxParticipants { get; set; }

        public int ParticipantsCount { get; set; }

        public EventStatus Status { get; set; }

        public string StatusText { get; set; } = string.Empty;

        public string StatusCssClass { get; set; } = string.Empty;

        public bool CanViewStatus { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string CreatorName { get; set; } = string.Empty;

        public bool IsFavorite { get; set; }

        public bool CanToggleFavorite { get; set; }
    }
}