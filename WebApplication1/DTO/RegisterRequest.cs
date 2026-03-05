using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTO
{
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = "";

        public string? Name { get; set; }
        public string? BirthDate { get; set; }
    }
}
