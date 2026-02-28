using LocalMeet.Models;

namespace LocalMeet.ViewModels
{
    public class EventListViewModel
    {
        public IEnumerable<Event> Events { get; set; } = new List<Event>();

        public IEnumerable<Category> Categories { get; set; } = new List<Category>();

        public int? SelectedCategoryId { get; set; }

        public string? Search { get; set; }

        public bool ShowPast { get; set; }

        public int Page { get; set; } = 1;

        public int TotalPages { get; set; }
    }
}