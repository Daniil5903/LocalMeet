using LocalMeet.Models.ViewModels.Events;

namespace LocalMeet.Models.ViewModels.Search
{
    public class SearchViewModel
    {
        public string? Query { get; set; }

        public List<EventListItemViewModel> Events { get; set; } = new();

        public List<SearchUserListItemViewModel> Users { get; set; } = new();

        public bool HasQuery => !string.IsNullOrWhiteSpace(Query);

        public bool HasResults => Events.Any() || Users.Any();
    }

    public class SearchUserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? AvatarPath { get; set; }

        public string? AboutPreview { get; set; }

        public bool IsPrivateProfile { get; set; }

        public bool CanViewPrivateInfo { get; set; }

        public DateTime RegistrationDate { get; set; }

        public int CreatedEventsCount { get; set; }

        public int ParticipationsCount { get; set; }
    }
}