using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTO
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
    }
}
