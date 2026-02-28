namespace LocalMeet.Models
{
    public class Participation
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int EventId { get; set; }
        public Event Event { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
