namespace WebApplication1.DTO
{
    public class MobileUserDto
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Name { get; set; }
        public int? Age { get; set; }
        public string? ImageData { get; set; }
        public string Role { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
