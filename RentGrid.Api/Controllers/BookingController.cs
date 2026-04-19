using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentGrid.Api.Data;
using RentGrid.Api.Dtos;
using RentGrid.Api.Models;

namespace RentGrid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public BookingController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<ActionResult<BookingCreatedDto>> CreateBooking([FromBody] CreateBookingDto dto)
    {
        if (dto is null)
        {
            return BadRequest("A foglalás adatai kötelezőek.");
        }

        if (dto.EndDate < dto.StartDate)
        {
            return BadRequest("Az EndDate nem lehet a StartDate előtt.");
        }

        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("A felhasználó azonosítása nem sikerült.");
        }

        var vehicle = await _dbContext.Vehicles.FindAsync(dto.VehicleId);
        if (vehicle is null)
        {
            return NotFound("A megadott jármű nem található.");
        }

        var overlapExists = await _dbContext.Bookings.AnyAsync(b =>
            b.VehicleId == dto.VehicleId &&
            dto.StartDate < b.EndDate &&
            dto.EndDate > b.StartDate);

        if (overlapExists)
        {
            return Conflict("A kiválasztott időszak már foglalt erre a járműre.");
        }

        var extraServices = await _dbContext.ExtraServices
            .Where(es => dto.ExtraServiceIds.Contains(es.Id))
            .ToListAsync();

        if (dto.ExtraServiceIds.Any() && extraServices.Count != dto.ExtraServiceIds.Distinct().Count())
        {
            return BadRequest("Érvénytelen extra szolgáltatás azonosító.");
        }

        var days = Math.Max(1, (dto.EndDate.Date - dto.StartDate.Date).Days);
        var totalPrice = days * vehicle.DailyPrice + extraServices.Sum(es => es.Price);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var booking = new Booking
            {
                UserId = userId,
                VehicleId = dto.VehicleId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                TotalPrice = totalPrice,
                Status = "Pending",
                BookingExtras = extraServices.Select(es => new BookingExtra
                {
                    ExtraServiceId = es.Id
                }).ToList()
            };

            await _dbContext.Bookings.AddAsync(booking);
            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            var response = new BookingCreatedDto
            {
                Id = booking.Id,
                VehicleId = booking.VehicleId,
                StartDate = booking.StartDate,
                EndDate = booking.EndDate,
                TotalPrice = booking.TotalPrice,
                Status = booking.Status
            };

            return Created($"api/booking/{booking.Id}", response);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBooking(int id)
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("A felhasználó azonosítása nem sikerült.");
        }

        var booking = await _dbContext.Bookings.FindAsync(id);
        if (booking is null)
        {
            return NotFound("A foglalás nem található.");
        }

        if (booking.UserId != userId)
        {
            return Forbid("Nincs jogosultsága a foglalás törléséhez.");
        }

        _dbContext.Bookings.Remove(booking);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("A felhasználó azonosítása nem sikerült.");
        }

        var booking = await _dbContext.Bookings.FindAsync(id);
        if (booking is null)
        {
            return NotFound("A foglalás nem található.");
        }

        if (booking.UserId != userId)
        {
            return Forbid("Nincs jogosultsága a foglalás módosításához.");
        }

        if (booking.Status == "Cancelled")
        {
            return BadRequest("A foglalás már törölve van.");
        }

        booking.Status = "Cancelled";
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin")]
    public async Task<ActionResult<IEnumerable<AdminBookingDto>>> GetAdminBookings()
    {
        var bookings = await _dbContext.Bookings
            .AsNoTracking()
            .Include(b => b.Vehicle)
            .Include(b => b.User)
            .Include(b => b.BookingExtras)
                .ThenInclude(be => be.ExtraService)
            .OrderByDescending(b => b.StartDate)
            .Select(b => new AdminBookingDto
            {
                Id = b.Id,
                VehicleId = b.VehicleId,
                VehicleBrand = b.Vehicle!.Brand,
                VehicleModel = b.Vehicle.Model,
                VehicleDailyPrice = b.Vehicle.DailyPrice,
                VehicleImageFileId = b.Vehicle.MongoImageIds.FirstOrDefault(),
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                TotalPrice = b.TotalPrice,
                Status = b.Status,
                UserEmail = b.User!.Email,
                UserFullName = b.User.FullName,
                Extras = b.BookingExtras.Select(be => new BookingExtraDto
                {
                    Id = be.ExtraServiceId,
                    Name = be.ExtraService!.Name,
                    Price = be.ExtraService.Price
                }).ToList()
            })
            .ToListAsync();

        return Ok(bookings);
    }

    [HttpGet("my-bookings")]
    public async Task<ActionResult<IEnumerable<MyBookingDto>>> GetMyBookings()
    {
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("A felhasználó azonosítása nem sikerült.");
        }

        var bookings = await _dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .Include(b => b.Vehicle)
            .Include(b => b.BookingExtras)
                .ThenInclude(be => be.ExtraService)
            .OrderByDescending(b => b.StartDate)
            .Select(b => new MyBookingDto
            {
                Id = b.Id,
                VehicleId = b.VehicleId,
                VehicleBrand = b.Vehicle!.Brand,
                VehicleModel = b.Vehicle.Model,
                VehicleDailyPrice = b.Vehicle.DailyPrice,
                VehicleImageFileId = b.Vehicle.MongoImageIds.FirstOrDefault(),
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                TotalPrice = b.TotalPrice,
                Status = b.Status,
                Extras = b.BookingExtras.Select(be => new BookingExtraDto
                {
                    Id = be.ExtraServiceId,
                    Name = be.ExtraService!.Name,
                    Price = be.ExtraService.Price
                }).ToList()
            })
            .ToListAsync();

        return Ok(bookings);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateBookingStatus(int id, [FromBody] UpdateBookingStatusDto dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.Status))
        {
            return BadRequest("A státusz megadása kötelező.");
        }

        var normalizedStatus = dto.Status.Trim();
        normalizedStatus = normalizedStatus.ToLowerInvariant() switch
        {
            "pending" => "Pending",
            "confirmed" => "Confirmed",
            "cancelled" => "Cancelled",
            "canceled" => "Cancelled",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(normalizedStatus))
        {
            return BadRequest("A státusz csak Pending, Confirmed vagy Cancelled lehet.");
        }

        var booking = await _dbContext.Bookings.FindAsync(id);
        if (booking is null)
        {
            return NotFound("A foglalás nem található.");
        }

        booking.Status = normalizedStatus;
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
