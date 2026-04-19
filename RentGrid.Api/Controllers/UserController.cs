using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentGrid.Api.Data;
using RentGrid.Api.Dtos;

namespace RentGrid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public UserController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var userIdValue = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userIdValue) || !int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized("Nem sikerült azonosítani a felhasználót a token alapján.");
        }

        var user = await _dbContext.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.FullName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive
            })
            .SingleOrDefaultAsync();

        if (user is null)
        {
            return NotFound("A felhasználó nem található.");
        }

        return Ok(user);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.FullName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPut("{id}/toggle-active")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserDto>> ToggleUserActive(int id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        
        if (user is null)
        {
            return NotFound("A felhasználó nem található.");
        }

        user.IsActive = !user.IsActive;
        await _dbContext.SaveChangesAsync();

        return Ok(new UserDto
        {
            Id = user.Id,
            Name = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive
        });
    }
}
