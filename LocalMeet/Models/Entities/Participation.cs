namespace LocalMeet.Models.Entities
{
    public class Participation
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public User? User { get; set; }

        public int EventId { get; set; }

        public Event? Event { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}