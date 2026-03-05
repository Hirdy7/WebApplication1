namespace WebApplication1.Models
{
    public class UserDevice
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public User User { get; set; }

        public string FcmToken { get; set; } = string.Empty;

        public string Platform { get; set; } = "Android";

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}
