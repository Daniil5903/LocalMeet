using LocalMeet.Models.Enums;

namespace LocalMeet.Models.ViewModels.Events
{
    public class DeleteEventViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public int ParticipantsCount { get; set; }

        public EventStatus Status { get; set; }

        public string StatusText { get; set; } = string.Empty;

        public string StatusCssClass { get; set; } = string.Empty;
    }
}