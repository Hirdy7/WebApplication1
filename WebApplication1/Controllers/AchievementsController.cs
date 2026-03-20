using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Data; // Твой ApplicationDbContext
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AchievementsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AchievementsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Achievement>>> GetAchievements()
        {
            return await _context.Achievements.ToListAsync();
        }

        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<Achievement>>> GetMyAchievements()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            var userId = Guid.Parse(userIdClaim.Value);

            var myAchievements = await _context.UserAchievements
                .Where(ua => ua.UserId == userId)
                .Select(ua => ua.Achievement) 
                .ToListAsync();

            return Ok(myAchievements);
        }

        [HttpPost]
        public async Task<ActionResult<Achievement>> PostAchievement(Achievement achivment)
        {
            _context.Achievements.Add(achivment);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAchievements), new { id = achivment.Id }, achivment);
        }
    }
}