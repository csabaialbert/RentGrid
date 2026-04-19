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
    public async Task<IActionResult> CreateVehicle([FromForm] CreateVehicleDto dto, [FromForm] List<IFormFile> images)
    {
        if (dto is null)
        {
            return BadRequest("A jármű adatai kötelezőek.");
        }

        if (images == null || images.Count == 0)
        {
            return BadRequest("Legalább egy kép feltöltése kötelező.");
        }

        if (images.Any(img => string.IsNullOrWhiteSpace(img.ContentType) || !img.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("Csak képfájlok tölthetők fel.");
        }

        var imageIds = new List<string>();

        try
        {
            foreach (var image in images)
            {
                using var stream = image.OpenReadStream();
                var imageId = await _imageService.UploadImageAsync(stream, image.FileName);
                imageIds.Add(imageId);
            }

            var vehicle = new Vehicle
            {
                Brand = dto.Brand,
                Model = dto.Model,
                DailyPrice = dto.DailyPrice,
                MongoImageIds = imageIds,
                IsAvailable = true
            };

            await _dbContext.Vehicles.AddAsync(vehicle);
            await _dbContext.SaveChangesAsync();

            return Created($"api/vehicle/{vehicle.Id}", vehicle);
        }
        catch (Exception ex)
        {
            // Töröljük a már feltöltött képeket hiba esetén
            foreach (var imageId in imageIds)
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
                ImageFileIds = v.MongoImageIds
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

    [Authorize(Roles = "Admin")]
    [HttpPatch("{id}/price")]
    public async Task<IActionResult> UpdateVehiclePrice(int id, [FromBody] UpdateVehiclePriceDto dto)
    {
        if (dto is null || dto.DailyPrice <= 0)
        {
            return BadRequest("Az érvényes napi ár megadása kötelező.");
        }

        var vehicle = await _dbContext.Vehicles.FindAsync(id);
        if (vehicle is null)
        {
            return NotFound("A jármű nem található.");
        }

        vehicle.DailyPrice = dto.DailyPrice;
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/images")]
    public async Task<IActionResult> AddVehicleImages(int id, [FromForm] List<IFormFile> images)
    {
        if (images == null || images.Count == 0)
        {
            return BadRequest("Legalább egy kép feltöltése kötelező.");
        }

        if (images.Any(img => string.IsNullOrWhiteSpace(img.ContentType) || !img.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("Csak képfájlok tölthetők fel.");
        }

        var vehicle = await _dbContext.Vehicles.FindAsync(id);
        if (vehicle is null)
        {
            return NotFound("A jármű nem található.");
        }

        var newImageIds = new List<string>();

        try
        {
            foreach (var image in images)
            {
                using var stream = image.OpenReadStream();
                var imageId = await _imageService.UploadImageAsync(stream, image.FileName);
                newImageIds.Add(imageId);
            }

            vehicle.MongoImageIds.AddRange(newImageIds);
            await _dbContext.SaveChangesAsync();

            return Ok(new { ImageIds = newImageIds });
        }
        catch (Exception ex)
        {
            // Töröljük a már feltöltött képeket hiba esetén
            foreach (var imageId in newImageIds)
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

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}/images/{imageId}")]
    public async Task<IActionResult> RemoveVehicleImage(int id, string imageId)
    {
        var vehicle = await _dbContext.Vehicles.FindAsync(id);
        if (vehicle is null)
        {
            return NotFound("A jármű nem található.");
        }

        if (!vehicle.MongoImageIds.Contains(imageId))
        {
            return BadRequest("A kép nem tartozik ehhez a járműhöz.");
        }

        try
        {
            await _imageService.DeleteImageAsync(imageId);
            vehicle.MongoImageIds.Remove(imageId);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
