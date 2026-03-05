using WebApplication1.Models;

namespace WebApplication1.DTO
{

    public class CreateReminderDto
    {
        public string Text { get; set; } = string.Empty;
        public DateTime TriggerAt { get; set; }     
    }


}
