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

        [Authorize(Policy = "ModeratorOnly")]
        [HttpGet("alladmin")]
        public async Task<IActionResult> GetAll()
        {
            var requests = await _context.DisposalRequests
                .Include(r => r.User)           // Загружаем данные пользователя
                .Include(r => r.DisposalPoint)  // Загружаем данные о точке
                .Include(r => r.WasteType)      // Загружаем данные о типе мусора
                .OrderByDescending(r => r.CreatedAt) // Сначала новые
                .Select(r => new
                {
                    r.Id,
                    UserName = r.User.Name,      // Или r.User.Email, смотря что у вас в модели User
                    PointName = r.DisposalPoint.Name,
                    WasteTypeName = r.WasteType.Name,
                    r.PhotoUrl,
                    r.Weight,
                    r.Comment,
                    Status = r.Status.ToString(), // Превращаем Enum в строку для удобства фронтенда
                    r.PointsAwarded,
                    r.CreatedAt,
                    r.ReviewedAt
                })
                .ToListAsync();

            return Ok(requests);
        }

        [HttpPost("send")]
        public async Task<IActionResult> CreateRequest([FromForm] CreateDisposalRequestDto dto)
        {
            // 1. Получение ID пользователя
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized(new { message = "Пользователь не авторизован" });
            }

            // 2. Проверка точки (Используем Guid, если в базе Guid)
            var pointExists = await _context.DisposalPoints.AnyAsync(p => p.Id == dto.DisposalPointId);
            if (!pointExists)
            {
                return BadRequest(new { message = "Точка не найдена" });
            }

            // 3. Проверка типа мусора (Исправляет ошибку внешнего ключа)
            var wasteTypeExists = await _context.WasteTypes.AnyAsync(w => w.Id == dto.WasteTypeId);
            if (!wasteTypeExists)
            {
                return BadRequest(new { message = "Указанный тип мусора не существует" });
            }

            // 4. Сохранение фото
            if (dto.Photo == null || dto.Photo.Length == 0)
                return BadRequest(new { message = "Фото обязательно" });

            string reportsFolder = Path.Combine(_env.WebRootPath, "reports");
            if (!Directory.Exists(reportsFolder)) Directory.CreateDirectory(reportsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Photo.FileName);
            string filePath = Path.Combine(reportsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await dto.Photo.CopyToAsync(fileStream);
            }

            // 5. Создание записи в БД
            var newRequest = new DisposalRequest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DisposalPointId = dto.DisposalPointId,
                WasteTypeId = dto.WasteTypeId, // ПЕРЕДАЕМ ID ТИПА МУСОРА
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
                return StatusCode(500, new { message = "Ошибка сохранения", details = ex.InnerException?.Message });
            }
        }

        // 1. Получить все заявки (для админ-панели)
        [HttpGet("all")]
        [Authorize(Policy = "ModeratorOnly")]
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
        [Authorize(Policy = "ModeratorOnly")]
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

            // Записываем ответ админа/модератора (причину)
            request.Response = dto.Response;
            request.ReviewedAt = DateTime.UtcNow;

            if (dto.NewStatus == DisposalRequestStatus.Approved)
            {
                if (request.WasteType == null)
                {
                    return BadRequest("Ошибка: тип отходов для этой заявки не определен в БД");
                }

                int pointsFromDb = request.WasteType.Rewards;
                request.User.TotalPoints += pointsFromDb;
                request.PointsAwarded = pointsFromDb;
                request.Status = DisposalRequestStatus.Approved;
            }
            else if (dto.NewStatus == DisposalRequestStatus.Rejected)
            {
                // Проверка: желательно, чтобы при отказе была указана причина
                if (string.IsNullOrWhiteSpace(dto.Response))
                {   
                    return BadRequest("При отказе необходимо указать причину в поле Response");
                }

                request.Status = DisposalRequestStatus.Rejected;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ошибка при сохранении изменений");
            }

            return Ok(new
            {
                message = $"Статус обновлен на {request.Status}",
                response = request.Response, // Возвращаем записанный текст для подтверждения
                pointsEarned = request.PointsAwarded ?? 0,
                totalUserPoints = request.User.TotalPoints
            });
        }


    }
}
