using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models; // Убедись, что путь к модели User верный
using MyDTO = WebApplication1.DTO;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- ПУБЛИЧНЫЕ МЕТОДЫ ---

        [HttpGet("leaderboard")]
        public async Task<ActionResult<List<MyDTO.UserLeaderboardDTO>>> GetLeaderboard()
        {
            var leaderboard = await _context.Users
                .OrderByDescending(u => u.TotalPoints)
                .Take(5)
                .Select(u => new MyDTO.UserLeaderboardDTO
                {
                    Username = u.Name,
                    Points = u.TotalPoints
                })
                .ToListAsync();

            return Ok(leaderboard);
        }

        // --- АДМИН-ПАНЕЛЬ (CRUD) ---

        // 1. Получить список всех пользователей
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // 2. Получить конкретного пользователя по ID
        [HttpGet("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<User>> GetUserById(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("Пользователь не найден");
            return user;
        }

        // 3. Обновить любые поля пользователя (баллы, имя, роль, email)
        [HttpPut("admin/update/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] User userUpdate)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Обновляем поля
            user.Name = userUpdate.Name;
            user.Email = userUpdate.Email;
            user.Role = userUpdate.Role;
            user.TotalPoints = userUpdate.TotalPoints;
            // user.PasswordHash = ... (пароль лучше менять через отдельный сервис с хэшированием)

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id)) return NotFound();
                else throw;
            }

            return Ok(new { message = "Данные пользователя обновлены" });
        }

        // 4. Удалить пользователя
        [HttpDelete("admin/delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Пользователь {user.Name} удален" });
        }

        // 5. Быстрое начисление баллов (удобно для админа)
        [HttpPatch("admin/add-points/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddPoints(Guid id, [FromBody] int points)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.TotalPoints += points;
            await _context.SaveChangesAsync();

            return Ok(new { newBalance = user.TotalPoints });
        }

        private bool UserExists(Guid id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}