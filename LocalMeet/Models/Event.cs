using System.ComponentModel.DataAnnotations;
using LocalMeet.Models.Enums;

namespace LocalMeet.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Location { get; set; } = string.Empty;

        public DateTime Date { get; set; }

        // Лимит участников
        public int? MaxParticipants { get; set; }

        public EventStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public string? CancelReason { get; set; }

        // Создатель
        [Required]
        public string CreatorId { get; set; } = string.Empty;

        public ApplicationUser? Creator { get; set; }

        // Категория
        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        // Участники
        public ICollection<Participation> Participations { get; set; }
            = new List<Participation>();
    }
}