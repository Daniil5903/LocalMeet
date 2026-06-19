namespace LocalMeet.Models.ViewModels.Notifications
{
    public class NotificationsIndexViewModel
    {
        public List<NotificationViewModel> Notifications { get; set; } = new();

        public int PageNumber { get; set; }

        public int TotalPages { get; set; }

        public int TotalItems { get; set; }

        public bool HasPreviousPage => PageNumber > 1;

        public bool HasNextPage => PageNumber < TotalPages;
    }
}