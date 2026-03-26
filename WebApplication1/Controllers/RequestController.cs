using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.DTO;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public RequestController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost("send")]
        public async Task<IActionResult> CreateRequest([FromForm] CreateDisposalRequestDto dto)
        {
            // 1. Безопасное получение ID пользователя
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Пользователь не авторизован или токен невалиден" });
            }

            // 2. ПРОВЕРКА СУЩЕСТВОВАНИЯ ТОЧКИ (Решает твою ошибку 500)
            var pointExists = await _context.DisposalPoints.AnyAsync(p => p.Id == dto.DisposalPointId);
            if (!pointExists)
            {
                return BadRequest(new { message = $"Точка с ID {dto.DisposalPointId} не найдена. Обновите карту в приложении." });
            }

            // 3. Обработка и сохранение фото
            if (dto.Photo == null || dto.Photo.Length == 0)
                return BadRequest(new { message = "Фото обязательно" });

            // Убедимся, что папка wwwroot/reports существует
            string reportsFolder = Path.Combine(_env.WebRootPath, "reports");
            if (!Directory.Exists(reportsFolder)) Directory.CreateDirectory(reportsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Photo.FileName);
            string filePath = Path.Combine(reportsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await dto.Photo.CopyToAsync(fileStream);
            }

            // 4. Создание записи в БД
            var newRequest = new DisposalRequest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DisposalPointId = dto.DisposalPointId,
                // Сохраняем тип мусора и комментарий
                Comment = string.IsNullOrWhiteSpace(dto.Comment)
                    ? dto.WasteType
                    : $"{dto.WasteType}: {dto.Comment}",
                PhotoUrl = $"/reports/{uniqueFileName}",
                Status = DisposalRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.DisposalRequests.Add(newRequest);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Заявка успешно отправлена", requestId = newRequest.Id });
            }
            catch (DbUpdateException ex)
            {
                // Дополнительная ловушка для ошибок базы
                return StatusCode(500, new { message = "Ошибка сохранения в базу данных", details = ex.InnerException?.Message });
            }
        }
    }
}
