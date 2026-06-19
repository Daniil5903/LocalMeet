namespace LocalMeet.Models.ViewModels.Events
{
    public class MyEventsViewModel
    {
        public List<EventListItemViewModel> CreatedEvents { get; set; } = new();

        public List<EventListItemViewModel> ParticipatingEvents { get; set; } = new();

        public List<EventListItemViewModel> PastEvents { get; set; } = new();
    }
}