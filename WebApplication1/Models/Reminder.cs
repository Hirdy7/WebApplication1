namespace WebApplication1.Models
{
    public class Reminder  
    {
        public Guid Id { get; set; }                   
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public string Text { get; set; } = string.Empty;
        public DateTime TriggerAt { get; set; }         
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsSent { get; set; } = false;       
        public bool IsRead { get; set; } = false;       
        public DateTime? SentAt { get; set; }          

       
    }
}
