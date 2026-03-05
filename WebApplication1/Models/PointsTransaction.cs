namespace WebApplication1.Models
{
    public class PointsTransaction
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public User User { get; set; }

        public int Points { get; set; }

        public string Reason { get; set; } = string.Empty;

        public Guid? DisposalRequestId { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}
