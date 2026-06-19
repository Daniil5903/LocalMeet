using LocalMeet.Models.Enums;

namespace LocalMeet.Models.ViewModels.Events
{
    public class EventDetailsViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public DateTime EventDate { get; set; }

        public DateTime CreatedAt { get; set; }

        public int MaxParticipants { get; set; }

        public int ParticipantsCount { get; set; }

        public bool IsFull => ParticipantsCount >= MaxParticipants;

        public EventStatus Status { get; set; }

        public string StatusText { get; set; } = string.Empty;

        public string StatusCssClass { get; set; } = string.Empty;

        public string? RejectReason { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string CreatorId { get; set; } = string.Empty;

        public string CreatorName { get; set; } = string.Empty;

        public string? CreatorAvatarPath { get; set; }

        public string? CurrentUserId { get; set; }

        public bool CanManage { get; set; }

        public bool IsAuthenticated { get; set; }

        public bool IsCreator { get; set; }

        public bool IsParticipant { get; set; }

        public bool CanJoin { get; set; }

        public bool CanLeave { get; set; }

        public bool IsFavorite { get; set; }

        public bool CanToggleFavorite { get; set; }

        public bool CanUseChat { get; set; }

        public bool IsAdmin { get; set; }

        public bool CanReportEvent { get; set; }

        public List<EventParticipantViewModel> Participants { get; set; } = new();

        public List<EventChatMessageViewModel> ChatMessages { get; set; } = new();
    }
}