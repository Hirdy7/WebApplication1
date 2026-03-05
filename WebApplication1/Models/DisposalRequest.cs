namespace WebApplication1.Models
{
    public enum DisposalRequestStatus
    {
        Pending,
        Approved,
        Rejected
    }
    public class DisposalRequest
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public User User { get; set; }

        public Guid DisposalPointId { get; set; }

        public DisposalPoint DisposalPoint { get; set; }

        public string PhotoUrl { get; set; } = string.Empty;

        public double? Weight { get; set; }

        public string? Comment { get; set; }

        public DisposalRequestStatus Status { get; set; }
            = DisposalRequestStatus.Pending;

        public int? PointsAwarded { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        public DateTime? ReviewedAt { get; set; }
    }
}
