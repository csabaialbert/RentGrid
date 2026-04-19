using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using NUnit.Framework;
using RentGrid.Api.Controllers;
using RentGrid.Api.Data;
using RentGrid.Api.Dtos;
using RentGrid.Api.Models;
using RentGrid.Api.Services;

namespace RentGrid.Api.Tests;

[TestFixture]
public class VehicleControllerTests
{
    private ApplicationDbContext _dbContext = null!;
    private Mock<IImageService> _mockImageService = null!;
    private VehicleController _controller = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _mockImageService = new Mock<IImageService>();
        _controller = new VehicleController(_dbContext, _mockImageService.Object);

        SeedDatabase();
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    private void SeedDatabase()
    {
        var vehicle1 = new Vehicle
        {
            Id = 1,
            Brand = "Toyota",
            Model = "Corolla",
            DailyPrice = 50.00m,
            IsAvailable = true,
            MongoImageIds = new List<string> { "image1" }
        };

        var vehicle2 = new Vehicle
        {
            Id = 2,
            Brand = "Honda",
            Model = "Civic",
            DailyPrice = 45.00m,
            IsAvailable = false,
            MongoImageIds = new List<string> { "image2" }
        };

        var vehicle3 = new Vehicle
        {
            Id = 3,
            Brand = "Ford",
            Model = "Focus",
            DailyPrice = 40.00m,
            IsAvailable = true,
            MongoImageIds = new List<string> { "image3" }
        };

        _dbContext.Vehicles.AddRange(vehicle1, vehicle2, vehicle3);
        _dbContext.SaveChanges();
    }

    private void SetupAdminUser()
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, "1"),
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private IFormFile CreateMockImageFile(string fileName = "test.jpg", string contentType = "image/jpeg")
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        var file = new Mock<IFormFile>();
        file.Setup(f => f.OpenReadStream()).Returns(stream);
        file.Setup(f => f.FileName).Returns(fileName);
        file.Setup(f => f.Length).Returns(stream.Length);
        file.Setup(f => f.ContentType).Returns(contentType);
        return file.Object;
    }

    [Test]
    public async Task CreateVehicle_ValidRequest_ReturnsCreatedResult()
    {
        SetupAdminUser();
        var dto = new CreateVehicleDto { Brand = "BMW", Model = "X3", DailyPrice = 80.00m };
        var imageFile = CreateMockImageFile();

        _mockImageService.Setup(s => s.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("uploaded-image-id");

        // FIX: Csomagolás List-be
        var result = await _controller.CreateVehicle(dto, new List<IFormFile> { imageFile });

        Assert.That(result, Is.InstanceOf<CreatedResult>());
        var createdResult = result as CreatedResult;
        Assert.That(createdResult!.Value, Is.InstanceOf<Vehicle>());

        var vehicle = createdResult.Value as Vehicle;
        Assert.That(vehicle!.Brand, Is.EqualTo("BMW"));
        Assert.That(vehicle.MongoImageIds, Does.Contain("uploaded-image-id"));

        _mockImageService.Verify(s => s.UploadImageAsync(It.IsAny<Stream>(), "test.jpg"), Times.Once);
    }

    [Test]
    public async Task CreateVehicle_NullDto_ReturnsBadRequest()
    {
        SetupAdminUser();
        var imageFile = CreateMockImageFile();

        // FIX: null! és List csomagolás
        var result = await _controller.CreateVehicle(null!, new List<IFormFile> { imageFile });

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateVehicle_NoImage_ReturnsBadRequest()
    {
        SetupAdminUser();
        var dto = new CreateVehicleDto { Brand = "BMW", Model = "X3", DailyPrice = 80.00m };

        // FIX: null! átadása
        var result = await _controller.CreateVehicle(dto, null!);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetAllVehicles_FilterByMinPrice_ReturnsFilteredVehicles()
    {
        var result = await _controller.GetAllVehicles(minPrice: 45.00m);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var vehicles = okResult!.Value as IEnumerable<VehicleDto>;

        Assert.That(vehicles, Is.Not.Null);
        // FIX: null-forgiving operator
        Assert.That(vehicles!.All(v => v.DailyPrice >= 45.00m), Is.True);
    }

    [Test]
    public async Task GetAllVehicles_FilterByMaxPrice_ReturnsFilteredVehicles()
    {
        var result = await _controller.GetAllVehicles(maxPrice: 45.00m);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var vehicles = okResult!.Value as IEnumerable<VehicleDto>;

        Assert.That(vehicles, Is.Not.Null);
        // FIX: null-forgiving operator
        Assert.That(vehicles!.All(v => v.DailyPrice <= 45.00m), Is.True);
    }

    [Test]
    public async Task GetAllVehicles_FilterByAvailability_ReturnsFilteredVehicles()
    {
        var result = await _controller.GetAllVehicles(isAvailable: true);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var vehicles = okResult!.Value as IEnumerable<VehicleDto>;

        Assert.That(vehicles, Is.Not.Null);
        // FIX: null-forgiving operator
        Assert.That(vehicles!.All(v => v.IsAvailable), Is.True);
    }

    [Test]
    public async Task GetAllVehicles_MultipleFilters_ReturnsFilteredVehicles()
    {
        var result = await _controller.GetAllVehicles(minPrice: 40.00m, maxPrice: 50.00m, isAvailable: true);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var vehicles = okResult!.Value as IEnumerable<VehicleDto>;

        Assert.That(vehicles, Is.Not.Null);
        // FIX: null-forgiving operator
        Assert.That(vehicles!.All(v => v.DailyPrice >= 40.00m && v.DailyPrice <= 50.00m && v.IsAvailable), Is.True);
    }

    [Test]
    public async Task GetImage_NullFileId_ReturnsBadRequest()
    {
        // FIX: null! átadása
        var result = await _controller.GetImage(null!);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }
}