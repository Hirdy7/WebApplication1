using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
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
            var points = await _context.DisposalPoints
                .Include(p => p.DisposalPointWasteTypes)
                .ThenInclude(pw => pw.WasteType)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Latitude,
                    p.Longitude,
                    p.Address,
                    p.PhotoUrl,
                    WasteTypes = p.DisposalPointWasteTypes
                        .Select(x => x.WasteType.Name)
                })
                .ToListAsync();

            return Ok(points);
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var point = await _context.DisposalPoints
                .Include(p => p.DisposalPointWasteTypes)
                .ThenInclude(pw => pw.WasteType)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (point == null)
                return NotFound();

            return Ok(point);
        }


        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] DisposalPoint model, IFormFile? image)
        {
            if (image != null && image.Length > 0)
            {
                var imagesPath = Path.Combine(_env.WebRootPath, "images");
                if (!Directory.Exists(imagesPath))
                    Directory.CreateDirectory(imagesPath);

                var ext = Path.GetExtension(image.FileName).ToLower();
                var fileName = $"{Guid.NewGuid()}{ext}";
                var savePath = Path.Combine(imagesPath, fileName);

                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                var request = HttpContext.Request;
                
                var host = request.Host.Value.Replace("localhost", "10.0.2.2");
                var baseUrl = $"{request.Scheme}://{host}";

                model.PhotoUrl = $"{baseUrl}/images/{fileName}";

            }

            _context.DisposalPoints.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }
        [HttpGet("{id}/waste-types")]
        public async Task<IActionResult> GetWasteTypes(Guid id)
        {
            var types = await _context.DisposalPointWasteTypes
                .Where(pw => pw.DisposalPointId == id)
                .Select(pw => pw.WasteType.Name) // Выбираем только названия
                .ToListAsync();

            return Ok(types);
        }

        [Authorize(Policy = "AdminOnly")]
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

        [Authorize(Policy = "AdminOnly")]
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



        [Authorize(Policy = "AdminOnly")]
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


        [Authorize(Policy = "AdminOnly")]
        [HttpPost("{id}/upload-photo")]
        public async Task<IActionResult> UploadPointPhoto(Guid id, IFormFile file)
        {
            // 1. Проверяем, существует ли такая точка
            var point = await _context.DisposalPoints.FindAsync(id);
            if (point == null) return NotFound("Точка сбора не найдена");

            // 2. Валидация файла
            if (file == null || file.Length == 0)
                return BadRequest("Файл не выбран или пуст");

            // 3. Подготовка пути (используем папку images, как в методе Create)
            var imagesPath = Path.Combine(_env.WebRootPath, "images");
            if (!Directory.Exists(imagesPath)) Directory.CreateDirectory(imagesPath);

            // 4. Генерация уникального имени
            var ext = Path.GetExtension(file.FileName).ToLower();
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(imagesPath, fileName);

            // 5. Сохранение файла на диск
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 6. Формирование URL (с учетом специфики Android эмулятора 10.0.2.2)
            var request = HttpContext.Request;
            var host = request.Host.Value.Replace("localhost", "10.0.2.2");
            var baseUrl = $"{request.Scheme}://{host}";

            // Сохраняем полный путь в базу
            point.PhotoUrl = $"{baseUrl}/images/{fileName}";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Фото точки успешно обновлено",
                photoUrl = point.PhotoUrl
            });
        }
    }
}