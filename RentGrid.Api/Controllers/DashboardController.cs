using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentGrid.Api.Data;
using RentGrid.Api.Dtos;

namespace RentGrid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public DashboardController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats()
    {
        var totalRevenue = await _dbContext.Bookings.SumAsync(b => (decimal?)b.TotalPrice) ?? 0m;
        var userCount = await _dbContext.Users.CountAsync();
        var now = DateTime.UtcNow;
        var activeBookingCount = await _dbContext.Bookings.CountAsync(b => b.StartDate <= now && b.EndDate >= now);

        var popularBooking = await _dbContext.Bookings
            .GroupBy(b => b.VehicleId)
            .Select(g => new
            {
                VehicleId = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(g => g.Count)
            .FirstOrDefaultAsync();

        PopularVehicleDto? popularVehicle = null;
        if (popularBooking is not null)
        {
            popularVehicle = await _dbContext.Vehicles
                .Where(v => v.Id == popularBooking.VehicleId)
                .Select(v => new PopularVehicleDto
                {
                    VehicleId = v.Id,
                    Brand = v.Brand,
                    Model = v.Model,
                    BookingCount = popularBooking.Count
                })
                .FirstOrDefaultAsync();
        }

        return Ok(new DashboardStatsDto
        {
            TotalRevenue = totalRevenue,
            RegisteredUserCount = userCount,
            ActiveBookingCount = activeBookingCount,
            MostPopularVehicle = popularVehicle
        });
    }
}
