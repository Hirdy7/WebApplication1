using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.DTO;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class DisposalPointsController : ControllerBase
    {

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env; 

        public DisposalPointsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // Динамический базовый URL (подстроится под эмулятор или сайт)
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var points = await _context.DisposalPoints
                .Include(p => p.DisposalPointWasteTypes)
                .ThenInclude(pw => pw.WasteType)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    // ОБЯЗАТЕЛЬНО ВОЗВРАЩАЕМ КООРДИНАТЫ (без них карта пустая)
                    p.Latitude,
                    p.Longitude,
                    p.Address,

                    // Исправленная логика фото
                    PhotoUrl = string.IsNullOrEmpty(p.PhotoUrl)
                        ? null
                        : (p.PhotoUrl.StartsWith("http")
                            ? p.PhotoUrl
                            : $"{baseUrl}{(p.PhotoUrl.StartsWith("/") ? "" : "/")}{p.PhotoUrl}"),

                    // ВОЗВРАЩАЕМ ТИПЫ (чтобы не было 404 при клике)
                    WasteTypes = p.DisposalPointWasteTypes
                        .Select(x => x.WasteType.Name)
                        .ToList()
                })
                .ToListAsync();

            return Ok(points);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var point = await _context.DisposalPoints.FindAsync(id);
            if (point == null) return NotFound();

            var host = Request.Host.Value.Replace("localhost", "10.0.2.2");
            var baseUrl = $"{Request.Scheme}://{host}";

            if (!string.IsNullOrEmpty(point.PhotoUrl))
            {
                point.PhotoUrl = baseUrl + point.PhotoUrl;
            }

            return Ok(point);
        }


        [Authorize(Policy = "ModeratorOnly")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] DisposalPoint model, IFormFile? image)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (image != null && image.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName).ToLower()}";
                var savePath = Path.Combine(_env.WebRootPath, "images", fileName);

                
                Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);

                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
                model.PhotoUrl = $"/images/{fileName}";
            }

            _context.DisposalPoints.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }



        [HttpGet("{id}/waste-types")]
        public async Task<ActionResult<IEnumerable<WasteTypeDto>>> GetWasteTypes(Guid id)
        {
            var wasteTypes = await _context.DisposalPointWasteTypes
                .Where(dpwt => dpwt.DisposalPointId == id)
                .Select(dpwt => new WasteTypeDto 
                {
                    Id = dpwt.WasteType.Id,
                    Name = dpwt.WasteType.Name,
                    Rewards = dpwt.WasteType.Rewards
                })
                .ToListAsync();

            if (!wasteTypes.Any()) return NotFound("Типы не найдены");

            return Ok(wasteTypes);
        }

        [Authorize(Policy = "ModeratorOnly")]
        [HttpPost("add-waste-type")]
        public async Task<IActionResult> AddWasteTypeToPoint(Guid pointId, Guid wasteTypeId)
        {
            var point = await _context.DisposalPoints.FindAsync(pointId);
            if (point == null) return NotFound("Точка не найдена");

            var wasteType = await _context.WasteTypes.FindAsync(wasteTypeId);
            if (wasteType == null) return NotFound("Тип отходов не найден");

          
            var link = new DisposalPointWasteType
            {
                DisposalPointId = pointId,
                WasteTypeId = wasteTypeId
            };

            _context.DisposalPointWasteTypes.Add(link);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Тип отходов успешно привязан к точке" });
        }

        [Authorize(Policy = "ModeratorOnly")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] DisposalPoint model)
        {
            var point = await _context.DisposalPoints.FindAsync(id);

            if (point == null)
                return NotFound();

            point.Name = model.Name;
            point.Latitude = model.Latitude;
            point.Longitude = model.Longitude;
            point.Address = model.Address;

           
            if (!string.IsNullOrEmpty(model.PhotoUrl))
            {
                point.PhotoUrl = model.PhotoUrl;
            }

            await _context.SaveChangesAsync();

            return Ok(point);
        }



        [Authorize(Policy = "ModeratorOnly")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var point = await _context.DisposalPoints.FindAsync(id);

            if (point == null)
                return NotFound();

            _context.DisposalPoints.Remove(point);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        [Authorize(Policy = "ModeratorOnly")]
        [HttpPost("{id}/upload-photo")]
        public async Task<IActionResult> UploadPointPhoto(Guid id, IFormFile file)
        {
            var point = await _context.DisposalPoints.FindAsync(id);
            if (point == null) return NotFound("Точка сбора не найдена");

            if (file == null || file.Length == 0)
                return BadRequest("Файл не выбран или пуст");

            // 1. Подготовка пути (внутри wwwroot/images)
            var imagesPath = Path.Combine(_env.WebRootPath, "images");
            if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);

            // 2. Генерация уникального имени
            var ext = Path.GetExtension(file.FileName).ToLower();
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(imagesPath, fileName);

            // 3. Сохранение файла на диск
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 4. СОХРАНЯЕМ В БАЗУ ТОЛЬКО ОТНОСИТЕЛЬНЫЙ ПУТЬ
            // Это залог того, что при смене домена или IP всё не сломается
            point.PhotoUrl = $"/images/{fileName}";
            await _context.SaveChangesAsync();

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var fullPhotoUrl = $"{baseUrl}{point.PhotoUrl}";

            return Ok(new
            {
                message = "Фото точки успешно обновлено",
                photoUrl = fullPhotoUrl // Возвращаем полный путь, чтобы мобила сразу отобразила
            });
        }
    }
}