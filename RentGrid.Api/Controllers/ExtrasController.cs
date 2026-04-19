using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentGrid.Api.Data;
using RentGrid.Api.Models;

namespace RentGrid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExtrasController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public ExtrasController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExtraService>>> GetAllExtras([FromQuery] bool includeInactive = false)
    {
        var query = _dbContext.ExtraServices.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(es => es.IsActive);
        }

        var extras = await query.ToListAsync();
        return Ok(extras);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ExtraService>> CreateExtra(CreateExtraDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Price <= 0)
        {
            return BadRequest("Az extra neve és ára kötelező, és az árnak nagyobbnak kell lennie nullánál.");
        }

        var extra = new ExtraService
        {
            Name = dto.Name.Trim(),
            Price = dto.Price,
            IsActive = true
        };

        _dbContext.ExtraServices.Add(extra);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAllExtras), new { includeInactive = true }, extra);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateExtra(int id, UpdateExtraDto dto)
    {
        var extra = await _dbContext.ExtraServices.FindAsync(id);
        if (extra == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Price <= 0)
        {
            return BadRequest("Az extra neve és ára kötelező, és az árnak nagyobbnak kell lennie nullánál.");
        }

        extra.Name = dto.Name.Trim();
        extra.Price = dto.Price;
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/activation")]
    public async Task<ActionResult> SetExtraActive(int id, SetExtraActiveDto dto)
    {
        var extra = await _dbContext.ExtraServices.FindAsync(id);
        if (extra == null)
        {
            return NotFound();
        }

        extra.IsActive = dto.IsActive;
        await _dbContext.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateExtraDto(string Name, decimal Price);
public record UpdateExtraDto(string Name, decimal Price);
public record SetExtraActiveDto(bool IsActive);
