namespace WebApplication1.Models
{
    public class SupportMessage
    {
        public Guid Id { get; set; }
        public Guid ChatId { get; set; }
        public SupportChat Chat { get; set; }

        public Guid SenderId { get; set; }
        public User Sender { get; set; }

        public string Text { get; set; } = string.Empty;
        public string? AttachmentUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; }

        // Очень поможет на фронтенде быстро понимать, кто прислал сообщение
        public bool IsSupportReply { get; set; }
    }
}
