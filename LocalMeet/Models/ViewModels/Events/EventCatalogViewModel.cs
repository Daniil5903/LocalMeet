using Microsoft.AspNetCore.Mvc.Rendering;

namespace LocalMeet.Models.ViewModels.Events
{
    public class EventCatalogViewModel
    {
        public List<EventListItemViewModel> Events { get; set; } = new();

        public string? SearchQuery { get; set; }

        public int? CategoryId { get; set; }

        public DateTime? EventDate { get; set; }

        public string? SortOrder { get; set; }

        public string EventPeriod { get; set; } = "upcoming";

        public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();

        public int PageNumber { get; set; }

        public int TotalPages { get; set; }

        public int TotalItems { get; set; }

        public bool HasPreviousPage => PageNumber > 1;

        public bool HasNextPage => PageNumber < TotalPages;
    }
}