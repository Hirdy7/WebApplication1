using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using MyDTO = WebApplication1.DTO;
using WebApplication1.DTO;

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

        [HttpGet("leaderboard")]
        public async Task<ActionResult<List<MyDTO.UserLeaderboardDTO>>> GetLeaderboard()
        {
            // Проверьте, что в БД поле действительно называется TotalPoints или Points
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
    }

}
