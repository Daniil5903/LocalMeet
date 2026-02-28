namespace LocalMeet.ViewModels
{
    public class AdminStatisticsViewModel
    {
        public int UsersCount { get; set; }
        public int EventsCount { get; set; }
        public int ApprovedEvents { get; set; }
        public int PendingEvents { get; set; }
        public int RejectedEvents { get; set; }
        public int CancelledEvents { get; set; }
        public int ParticipationsCount { get; set; }
    }
}