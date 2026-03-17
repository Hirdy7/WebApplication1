using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class User
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public int? Age { get; set; }

        public string? photoUrl { get; set; }
        
        public string Role { get; set; } = "User";

        public int TotalPoints { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<DisposalRequest> DisposalRequests { get; set; } = new List<DisposalRequest>();

        public ICollection<PointsTransaction> PointsTransactions { get; set; } = new List<PointsTransaction>();

        public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();

        public ICollection<SupportChat> SupportChats { get; set; } = new List<SupportChat>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        public ICollection<UserDevice> Devices { get; set; } = new List<UserDevice>();
    }
}
