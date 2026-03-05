namespace WebApplication1.Models
{
    public class SupportChat
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public User User { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;

        public ICollection<SupportMessage> Messages { get; set; }
            = new List<SupportMessage>();
    }
}
