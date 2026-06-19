using Microsoft.AspNetCore.Identity;

namespace LocalMeet.Models.Entities
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? AvatarPath { get; set; }

        public string? About { get; set; }

        public bool IsPrivateProfile { get; set; } = false;

        public bool IsBlocked { get; set; } = false;

        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        public DateTime? LastVisit { get; set; }

        public ICollection<Event> CreatedEvents { get; set; } = new List<Event>();

        public ICollection<Participation> Participations { get; set; } = new List<Participation>();

        public ICollection<FavoriteEvent> FavoriteEvents { get; set; } = new List<FavoriteEvent>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        public ICollection<EventMessage> EventMessages { get; set; } = new List<EventMessage>();

        public ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}