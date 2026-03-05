using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

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
                    u.TotalPoints
                })
                .FirstOrDefaultAsync();

            if (profile == null)
                return NotFound();

            return Ok(profile);
        }
    }
}