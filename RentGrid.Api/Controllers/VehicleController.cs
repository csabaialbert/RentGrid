using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RentGrid.Api.Data;
using RentGrid.Api.Dtos;
using RentGrid.Api.Models;
using RentGrid.Api.Services;

namespace RentGrid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VehicleController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IImageService _imageService;

    public VehicleController(ApplicationDbContext dbContext, IImageService imageService)
    {
        _dbContext = dbContext;
        _imageService = imageService;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateVehicle([FromForm] CreateVehicleDto dto, [FromForm] IFormFile image)
    {
        if (dto is null)
        {
            return BadRequest("A jármű adatai kötelezőek.");
        }

        if (image == null || image.Length == 0)
        {
            return BadRequest("A kép feltöltése kötelező.");
        }

        if (string.IsNullOrWhiteSpace(image.ContentType) || !image.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Csak képfájl tölthető fel.");
        }

        string? imageId = null;

        try
        {
            using var stream = image.OpenReadStream();
            imageId = await _imageService.UploadImageAsync(stream, image.FileName);

            var vehicle = new Vehicle
            {
                Brand = dto.Brand,
                Model = dto.Model,
                DailyPrice = dto.DailyPrice,
                MongoImageId = imageId,
                IsAvailable = true
            };

            await _dbContext.Vehicles.AddAsync(vehicle);
            await _dbContext.SaveChangesAsync();

            return Created($"api/vehicle/{vehicle.Id}", vehicle);
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrWhiteSpace(imageId))
            {
                try
                {
                    await _imageService.DeleteImageAsync(imageId);
                }
                catch
                {
                    // A tisztítás sikertelensége esetén sem dobunk újabb hibát.
                }
            }

            return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VehicleDto>>> GetAllVehicles(
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] bool? isAvailable = null)
    {
        var query = _dbContext.Vehicles.AsNoTracking().AsQueryable();

        if (minPrice.HasValue)
        {
            query = query.Where(v => v.DailyPrice >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(v => v.DailyPrice <= maxPrice.Value);
        }

        if (isAvailable.HasValue)
        {
            query = query.Where(v => v.IsAvailable == isAvailable.Value);
        }

        var vehicles = await query
            .Select(v => new VehicleDto
            {
                Id = v.Id,
                Brand = v.Brand,
                Model = v.Model,
                DailyPrice = v.DailyPrice,
                IsAvailable = v.IsAvailable,
                ImageFileId = v.MongoImageId
            })
            .ToListAsync();

        return Ok(vehicles);
    }

    [HttpGet("image/{fileId}")]
    public async Task<IActionResult> GetImage(string fileId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
        {
            return BadRequest("A fileId megadása kötelező.");
        }

        try
        {
            var stream = await _imageService.GetImageStreamAsync(fileId);
            return File(stream, "image/jpeg");
        }
        catch (ArgumentException)
        {
            return BadRequest("Érvénytelen fileId formátum.");
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
