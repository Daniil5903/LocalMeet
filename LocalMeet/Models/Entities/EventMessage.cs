namespace LocalMeet.Models.Entities
{
    public class EventMessage
    {
        public int Id { get; set; }

        public int EventId { get; set; }

        public Event? Event { get; set; }

        public string UserId { get; set; } = string.Empty;

        public User? User { get; set; }

        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsDeleted { get; set; } = false;
    }
}