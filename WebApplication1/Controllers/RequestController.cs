using Microsoft.AspNetCore.Authorization;
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
                return StatusCode(500, new { message = "Ошибка сохранения в базу данных", details = ex.InnerException?.Message });
            }
        }

        // 1. Получить все заявки (для админ-панели)
        [HttpGet("all")]
        [Authorize(Roles = "Admin")] // Только для админов
        public async Task<IActionResult> GetAllRequests()
        {
            var requests = await _context.DisposalRequests
                .Include(r => r.User) // Чтобы видеть кто отправил
                .Include(r => r.DisposalPoint) // Чтобы видеть куда принесли
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    UserName = r.User.Name,
                    PointName = r.DisposalPoint.Name,
                    r.Comment,
                    r.PhotoUrl,
                    r.Status,
                    r.CreatedAt
                })
                .ToListAsync();

            return Ok(requests);
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusDto dto)
        {
            var request = await _context.DisposalRequests
                .Include(r => r.User)
                .Include(r => r.WasteType)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound("Заявка не найдена");

            if (request.Status != DisposalRequestStatus.Pending)
            {
                return BadRequest("Эта заявка уже была обработана ранее");
            }

            if (dto.NewStatus == DisposalRequestStatus.Approved)
            {
                if (request.WasteType == null)
                {
                    return BadRequest("Ошибка: тип отходов для этой заявки не определен в БД");
                }

                // Берем баллы из справочника WasteTypes
                int pointsFromDb = request.WasteType.Rewards;

                // Начисляем пользователю
                request.User.TotalPoints += pointsFromDb;

                // Сохраняем в заявку для истории
                request.PointsAwarded = pointsFromDb;
                request.Status = DisposalRequestStatus.Approved;
                request.ReviewedAt = DateTime.UtcNow;
            }
            else if (dto.NewStatus == DisposalRequestStatus.Rejected)
            {
                request.Status = DisposalRequestStatus.Rejected;
                request.ReviewedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Статус обновлен на {request.Status}",
                pointsEarned = request.PointsAwarded ?? 0,
                totalUserPoints = request.User.TotalPoints
            });
        }
    }
}
