using LocalMeet.Models.Enums;

namespace LocalMeet.Models.Entities
{
    public class Report
    {
        public int Id { get; set; }

        public string AuthorId { get; set; } = string.Empty;

        public User? Author { get; set; }

        public ReportTargetType TargetType { get; set; }

        public string TargetId { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public ReportStatus Status { get; set; } = ReportStatus.New;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ReviewedAt { get; set; }

        public string? AdminComment { get; set; }
    }
}