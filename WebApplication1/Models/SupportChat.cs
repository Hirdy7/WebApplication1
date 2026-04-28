namespace WebApplication1.Models
{
    public class SupportChat
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; }

        // Поможет модератору фильтровать активные обращения
        public bool IsClosed { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow; // Для сортировки списка чатов

        public ICollection<SupportMessage> Messages { get; set; } = new List<SupportMessage>();
    }
}
