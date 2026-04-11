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
    public async Task<ActionResult<IEnumerable<ExtraService>>> GetAllExtras()
    {
        var extras = await _dbContext.ExtraServices
            .AsNoTracking()
            .ToListAsync();

        return Ok(extras);
    }
}
