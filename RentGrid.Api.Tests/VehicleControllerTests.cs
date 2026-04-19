using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory;
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
    private ApplicationDbContext _dbContext;
    private Mock<IImageService> _mockImageService;
    private VehicleController _controller;

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

        // Seed data
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
        // Arrange
        SetupAdminUser();
        var dto = new CreateVehicleDto
        {
            Brand = "BMW",
            Model = "X3",
            DailyPrice = 80.00m
        };
        var imageFile = CreateMockImageFile();

        _mockImageService.Setup(s => s.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync("uploaded-image-id");

        // Act
        var result = await _controller.CreateVehicle(dto, imageFile);

        // Assert
        Assert.That(result, Is.InstanceOf<CreatedResult>());
        var createdResult = result as CreatedResult;
        Assert.That(createdResult!.Value, Is.InstanceOf<Vehicle>());

        var vehicle = createdResult.Value as Vehicle;
        Assert.That(vehicle!.Brand, Is.EqualTo("BMW"));
        Assert.That(vehicle.Model, Is.EqualTo("X3"));
        Assert.That(vehicle.DailyPrice, Is.EqualTo(80.00m));
        Assert.That(vehicle.MongoImageIds, Does.Contain("uploaded-image-id"));
        Assert.That(vehicle.IsAvailable, Is.True);

        _mockImageService.Verify(s => s.UploadImageAsync(It.IsAny<Stream>(), "test.jpg"), Times.Once);
    }

    [Test]
    public async Task CreateVehicle_NullDto_ReturnsBadRequest()
    {
        // Arrange
        SetupAdminUser();
        var imageFile = CreateMockImageFile();

        // Act
        var result = await _controller.CreateVehicle(null, imageFile);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateVehicle_NoImage_ReturnsBadRequest()
    {
        // Arrange
        SetupAdminUser();
        var dto = new CreateVehicleDto
        {
            Brand = "BMW",
            Model = "X3",
            DailyPrice = 80.00m
        };

        // Act
        var result = await _controller.CreateVehicle(dto, null);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateVehicle_EmptyImage_ReturnsBadRequest()
    {
        // Arrange
        SetupAdminUser();
        var dto = new CreateVehicleDto
        {
            Brand = "BMW",
            Model = "X3",
            DailyPrice = 80.00m
        };
        var emptyImage = new Mock<IFormFile>();
        emptyImage.Setup(f => f.Length).Returns(0);

        // Act
        var result = await _controller.CreateVehicle(dto, emptyImage.Object);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateVehicle_InvalidImageType_ReturnsBadRequest()
    {
        // Arrange
        SetupAdminUser();
        var dto = new CreateVehicleDto
        {
            Brand = "BMW",
            Model = "X3",
            DailyPrice = 80.00m
        };
        var textFile = CreateMockImageFile("test.txt", "text/plain");

        // Act
        var result = await _controller.CreateVehicle(dto, textFile);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateVehicle_ImageUploadFails_DeletesImageAndReturnsError()
    {
        // Arrange
        SetupAdminUser();
        var dto = new CreateVehicleDto
        {
            Brand = "BMW",
            Model = "X3",
            DailyPrice = 80.00m
        };
        var imageFile = CreateMockImageFile();

        _mockImageService.Setup(s => s.UploadImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Upload failed"));

        // Act
        var result = await _controller.CreateVehicle(dto, imageFile);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = result as ObjectResult;
        Assert.That(objectResult!.StatusCode, Is.EqualTo(500));

        _mockImageService.Verify(s => s.DeleteImageAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task GetAllVehicles_NoFilters_ReturnsAllVehicles()
    {
        // Act
        var result = await _controller.GetAllVehicles();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult!.Value, Is.InstanceOf<IEnumerable<VehicleDto>>());

        var vehicles = okResult.Value as IEnumerable<VehicleDto>;
        Assert.That(vehicles, Is.Not.Null);
        Assert.That(vehicles!.Count(), Is.EqualTo(3));

        // Verify all vehicles are returned
        var vehicleIds = vehicles.Select(v => v.Id).OrderBy(id => id).ToList();
        Assert.That(vehicleIds, Is.EqualTo(new List<int> { 1, 2, 3 }));
    }

    [Test]
    public async Task GetAllVehicles_FilterByMinPrice_ReturnsFilteredVehicles()
    {
        // Act
        var result = await _controller.GetAllVehicles(minPrice: 45.00m);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var vehicles = okResult!.Value as IEnumerable<VehicleDto>;

        Assert.That(vehicles!.Count(), Is.EqualTo(2)); // Vehicles with price >= 45
        Assert.That(vehicles.All(v => v.DailyPrice >= 45.00m), Is.True);
    }

    [Test]
    public async Task GetAllVehicles_FilterByMaxPrice_ReturnsFilteredVehicles()
    {
        // Act
        var result = await _controller.GetAllVehicles(maxPrice: 45.00m);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var vehicles = okResult!.Value as IEnumerable<VehicleDto>;

        Assert.That(vehicles!.Count(), Is.EqualTo(2)); // Vehicles with price <= 45
        Assert.That(vehicles.All(v => v.DailyPrice <= 45.00m), Is.True);
    }

    [Test]
    public async Task GetAllVehicles_FilterByAvailability_ReturnsFilteredVehicles()
    {
        // Act
        var result = await _controller.GetAllVehicles(isAvailable: true);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var vehicles = okResult!.Value as IEnumerable<VehicleDto>;

        Assert.That(vehicles!.Count(), Is.EqualTo(2)); // Available vehicles
        Assert.That(vehicles.All(v => v.IsAvailable), Is.True);
    }

    [Test]
    public async Task GetAllVehicles_MultipleFilters_ReturnsFilteredVehicles()
    {
        // Act
        var result = await _controller.GetAllVehicles(minPrice: 40.00m, maxPrice: 50.00m, isAvailable: true);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var vehicles = okResult!.Value as IEnumerable<VehicleDto>;

        Assert.That(vehicles!.Count(), Is.EqualTo(2)); // Toyota and Ford (both available, prices in range)
        Assert.That(vehicles.All(v => v.DailyPrice >= 40.00m && v.DailyPrice <= 50.00m && v.IsAvailable), Is.True);
    }

    [Test]
    public async Task GetImage_ValidFileId_ReturnsFileResult()
    {
        // Arrange
        var expectedStream = new MemoryStream(new byte[] { 1, 2, 3 });
        _mockImageService.Setup(s => s.GetImageStreamAsync("valid-id"))
            .ReturnsAsync(expectedStream);

        // Act
        var result = await _controller.GetImage("valid-id");

        // Assert
        Assert.That(result, Is.InstanceOf<FileStreamResult>());
        var fileResult = result as FileStreamResult;
        Assert.That(fileResult!.ContentType, Is.EqualTo("image/jpeg"));

        _mockImageService.Verify(s => s.GetImageStreamAsync("valid-id"), Times.Once);
    }

    [Test]
    public async Task GetImage_EmptyFileId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetImage("");

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetImage_NullFileId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetImage(null);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetImage_ImageNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockImageService.Setup(s => s.GetImageStreamAsync("invalid-id"))
            .ThrowsAsync(new InvalidOperationException("Image not found"));

        // Act
        var result = await _controller.GetImage("invalid-id");

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task GetImage_InvalidFileIdFormat_ReturnsBadRequest()
    {
        // Arrange
        _mockImageService.Setup(s => s.GetImageStreamAsync("invalid-format"))
            .ThrowsAsync(new ArgumentException("Invalid format"));

        // Act
        var result = await _controller.GetImage("invalid-format");

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetImage_ServiceError_ReturnsInternalServerError()
    {
        // Arrange
        _mockImageService.Setup(s => s.GetImageStreamAsync("error-id"))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetImage("error-id");

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var objectResult = result as ObjectResult;
        Assert.That(objectResult!.StatusCode, Is.EqualTo(500));
    }
}