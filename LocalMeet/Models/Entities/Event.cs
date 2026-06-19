using LocalMeet.Models.Enums;

namespace LocalMeet.Models.Entities
{
    public class Event
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        public DateTime EventDate { get; set; }

        public int MaxParticipants { get; set; }

        public EventStatus Status { get; set; } = EventStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? RejectReason { get; set; }

        public string CreatorId { get; set; } = string.Empty;

        public User? Creator { get; set; }

        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        public ICollection<Participation> Participations { get; set; } = new List<Participation>();

        public ICollection<FavoriteEvent> FavoriteEvents { get; set; } = new List<FavoriteEvent>();

        public ICollection<EventMessage> Messages { get; set; } = new List<EventMessage>();
    }
}