using LocalMeet.Models.Enums;

namespace LocalMeet.Models.ViewModels.Admin
{
    public class AdminEventListItemViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public string CreatorName { get; set; } = string.Empty;

        public string CreatorEmail { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public DateTime CreatedAt { get; set; }

        public EventStatus Status { get; set; }

        public string StatusText { get; set; } = string.Empty;

        public string StatusCssClass { get; set; } = string.Empty;
    }
}