using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DTO;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/mobile")]
    [Authorize(Policy = "UserOnly")]
    public class MobileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MobileController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdString, out var userId))
                return Unauthorized();

            var profile = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.TotalPoints,
                    u.photoUrl
                })
                .FirstOrDefaultAsync();

            if (profile == null)
                return NotFound();

            return Ok(profile);
        }

        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

            if (file == null || file.Length == 0) return BadRequest("Файл не выбран");

       
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "avatars");
            if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

           
            var fileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsPath, fileName);

          
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

          
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            
            user.photoUrl = $"avatars/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new { photoUrl = user.photoUrl });
        }

        [HttpPost("change-nickname")]
        public async Task<IActionResult> ChangeNickname([FromBody] ChangeNicknameDto dto)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdString, out var userId))
                return Unauthorized();

            var result = new NicknameChangeResultDto();

            // 1. Проверка на пустоту
            if (string.IsNullOrWhiteSpace(dto.NewNickname))
            {
                result.IsSuccess = false;
                result.Message = "Никнейм не может быть пустым или состоять из пробелов.";
                return BadRequest(result);
            }

            // 2. Проверка длины
            if (dto.NewNickname.Length < 3 || dto.NewNickname.Length > 20)
            {
                result.IsSuccess = false;
                result.Message = "Длина никнейма должна быть от 3 до 20 символов.";
                return BadRequest(result);
            }

            // 3. Поиск пользователя
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                result.IsSuccess = false;
                result.Message = "Пользователь не найден в системе.";
                return NotFound(result);
            }

            // 4. Проверка на идентичность (если ввел тот же самый ник)
            if (user.Name == dto.NewNickname)
            {
                result.IsSuccess = false;
                result.Message = "Новый никнейм совпадает с текущим.";
                return BadRequest(result);
            }

            // 5. Проверка на уникальность в БД
            var isTaken = await _context.Users.AnyAsync(u => u.Name == dto.NewNickname);
            if (isTaken)
            {
                result.IsSuccess = false;
                result.Message = "Этот никнейм уже занят другим пользователем.";
                return Conflict(result); // 409 Conflict лучше подходит для занятых имен
            }

            // УСПЕХ
            user.Name = dto.NewNickname;
            await _context.SaveChangesAsync();

            result.IsSuccess = true;
            result.Message = "Никнейм успешно изменен!";
            result.NewNickname = user.Name;

            return Ok(result);
        }
    }
}