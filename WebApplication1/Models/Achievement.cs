namespace WebApplication1.Models
{
    public class Achievement
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string PhotoUrl { get; set; }
        public string Description { get; set; }
        public string Condition { get; set; }
        public int Award { get; set; }

    }
}
