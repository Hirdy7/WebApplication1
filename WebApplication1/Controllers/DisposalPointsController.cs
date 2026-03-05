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

        public DisposalPointsController(ApplicationDbContext context)
        {
            _context = context;
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
        public async Task<IActionResult> Create(DisposalPoint model)
        {
            _context.DisposalPoints.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        
        [Authorize(Policy = "AdminOnly")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, DisposalPoint model)
        {
            var point = await _context.DisposalPoints.FindAsync(id);

            if (point == null)
                return NotFound();

            point.Name = model.Name;
            point.Latitude = model.Latitude;
            point.Longitude = model.Longitude;
            point.Address = model.Address;

            await _context.SaveChangesAsync();

            return NoContent();
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
    }
}