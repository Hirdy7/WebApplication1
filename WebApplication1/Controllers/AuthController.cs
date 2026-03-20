using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Service;
using Microsoft.AspNetCore.Identity.Data;
using MyDTO = WebApplication1.DTO;


namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(MyDTO.RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(result.Data);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(MyDTO.LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            if (!result.IsSuccess)
                return Unauthorized(result.Error);

            return Ok(result.Data);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] MyDTO.ForgotPasswordRequest request)
        {
            var result = await _authService.ForgotPasswordAsync(request.Email);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(new { message = "Код восстановления отправлен на вашу почту" });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(WebApplication1.DTO.ResetPasswordRequest request)
        {
            
            var result = await _authService.ResetPasswordAsync(request);

            if (!result.IsSuccess)
                return BadRequest(result.Error);

            return Ok(new { message = "Пароль успешно изменен" });
        }
    }
}