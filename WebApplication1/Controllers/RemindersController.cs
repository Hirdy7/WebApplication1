using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.DTO;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RemindersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RemindersController(ApplicationDbContext context)
        {
            _context = context;
        }

        private Guid GetUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.Parse(userIdString!);
        }

       

        [HttpGet]
        public async Task<IActionResult> GetAllbyUser()
        {
            var userId = GetUserId();

            var reminders = await _context.Reminders
                .Where(r => r.UserId == userId)
                .OrderBy(r => r.TriggerAt)
                .ToListAsync();

            return Ok(reminders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = GetUserId();

            var reminder = await _context.Reminders
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reminder == null)
                return NotFound();

            return Ok(reminder);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateReminderDto dto)
        {

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdString, out var currentUserId))
                return Unauthorized();


            if (dto.TriggerAt < DateTime.UtcNow.AddMinutes(5))
                return BadRequest("Напоминание должно быть не раньше чем через 5 минут");

            if (dto.TriggerAt > DateTime.UtcNow.AddYears(1))
                return BadRequest("Напоминание нельзя ставить больше чем на год вперёд");


            if (string.IsNullOrWhiteSpace(dto.Text) || dto.Text.Length > 500)
                return BadRequest("Текст обязателен и не длиннее 500 символов");

            var reminder = new Reminder
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                Text = dto.Text.Trim(),
                TriggerAt = dto.TriggerAt,
                CreatedAt = DateTime.UtcNow,
                IsSent = false,
                IsRead = false
            };

            _context.Reminders.Add(reminder);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = reminder.Id }, reminder);
        }

       
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, CreateReminderDto dto)
        {
            var userId = GetUserId();

            var reminder = await _context.Reminders
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reminder == null)
                return NotFound();

            reminder.Text = dto.Text.Trim();
            reminder.TriggerAt = dto.TriggerAt;

            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpGet("alluser")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAllUser()
        {
            var reminders = await _context.Reminders
                .Select(p => new
                {
                    p.Id,
                    p.UserId,
                    p.Text,
                    p.TriggerAt,
                    p.CreatedAt,
                    p.IsSent,
                    p.IsRead,
                    p.SentAt
                })
                .ToListAsync();

            return Ok(reminders);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetUserId();

            var reminder = await _context.Reminders
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (reminder == null)
                return NotFound();

            _context.Reminders.Remove(reminder);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}