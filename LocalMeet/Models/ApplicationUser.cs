using Microsoft.AspNetCore.Identity;

namespace LocalMeet.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsBlocked { get; set; }

        public ICollection<Event> CreatedEvents { get; set; }
        public ICollection<Participation> Participations { get; set; }
    }
}