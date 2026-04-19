using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory;
using NUnit.Framework;
using RentGrid.Api.Controllers;
using RentGrid.Api.Data;
using RentGrid.Api.Dtos;
using RentGrid.Api.Models;

namespace RentGrid.Api.Tests;

[TestFixture]
public class BookingControllerTests
{
    private ApplicationDbContext _dbContext;
    private BookingController _controller;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _controller = new BookingController(_dbContext);

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
        var user = new User
        {
            Id = 1,
            FullName = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash"
        };

        var vehicle = new Vehicle
        {
            Id = 1,
            Brand = "Toyota",
            Model = "Corolla",
            DailyPrice = 50,
            IsAvailable = true,
            MongoImageIds = new List<string> { "image1" }
        };

        var extraService1 = new ExtraService
        {
            Id = 1,
            Name = "GPS",
            Price = 10
        };

        var extraService2 = new ExtraService
        {
            Id = 2,
            Name = "Insurance",
            Price = 20
        };

        _dbContext.Users.Add(user);
        _dbContext.Vehicles.Add(vehicle);
        _dbContext.ExtraServices.Add(extraService1);
        _dbContext.ExtraServices.Add(extraService2);
        _dbContext.SaveChanges();
    }

    private void SetupUser(int userId)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Test]
    public async Task CreateBooking_ValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        SetupUser(1);
        var dto = new CreateBookingDto
        {
            VehicleId = 1,
            StartDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now.AddDays(3),
            ExtraServiceIds = new List<int> { 1 }
        };

        // Act
        var result = await _controller.CreateBooking(dto);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<CreatedResult>());
        var createdResult = result.Result as CreatedResult;
        Assert.That(createdResult!.Value, Is.InstanceOf<BookingCreatedDto>());
        var response = createdResult.Value as BookingCreatedDto;
        Assert.That(response!.VehicleId, Is.EqualTo(1));
        Assert.That(response.Status, Is.EqualTo("Pending"));
        Assert.That(response.TotalPrice, Is.EqualTo(110)); // 2 days * 50 + 10
    }

    [Test]
    public async Task CreateBooking_NullDto_ReturnsBadRequest()
    {
        // Arrange
        SetupUser(1);

        // Act
        var result = await _controller.CreateBooking(null);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateBooking_EndDateBeforeStartDate_ReturnsBadRequest()
    {
        // Arrange
        SetupUser(1);
        var dto = new CreateBookingDto
        {
            VehicleId = 1,
            StartDate = DateTime.Now.AddDays(3),
            EndDate = DateTime.Now.AddDays(1),
            ExtraServiceIds = new List<int>()
        };

        // Act
        var result = await _controller.CreateBooking(dto);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateBooking_InvalidVehicleId_ReturnsNotFound()
    {
        // Arrange
        SetupUser(1);
        var dto = new CreateBookingDto
        {
            VehicleId = 999,
            StartDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now.AddDays(3),
            ExtraServiceIds = new List<int>()
        };

        // Act
        var result = await _controller.CreateBooking(dto);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task CreateBooking_OverlappingBooking_ReturnsConflict()
    {
        // Arrange
        SetupUser(1);
        var existingBooking = new Booking
        {
            UserId = 1,
            VehicleId = 1,
            StartDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now.AddDays(3),
            TotalPrice = 100,
            Status = "Pending"
        };
        _dbContext.Bookings.Add(existingBooking);
        await _dbContext.SaveChangesAsync();

        var dto = new CreateBookingDto
        {
            VehicleId = 1,
            StartDate = DateTime.Now.AddDays(2),
            EndDate = DateTime.Now.AddDays(4),
            ExtraServiceIds = new List<int>()
        };

        // Act
        var result = await _controller.CreateBooking(dto);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ConflictObjectResult>());
    }

    [Test]
    public async Task CreateBooking_InvalidExtraServiceId_ReturnsBadRequest()
    {
        // Arrange
        SetupUser(1);
        var dto = new CreateBookingDto
        {
            VehicleId = 1,
            StartDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now.AddDays(3),
            ExtraServiceIds = new List<int> { 1, 999 }
        };

        // Act
        var result = await _controller.CreateBooking(dto);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task DeleteBooking_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        SetupUser(1);
        var booking = new Booking
        {
            UserId = 1,
            VehicleId = 1,
            StartDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now.AddDays(3),
            TotalPrice = 100,
            Status = "Pending"
        };
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteBooking(booking.Id);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
        var deletedBooking = await _dbContext.Bookings.FindAsync(booking.Id);
        Assert.That(deletedBooking, Is.Null);
    }

    [Test]
    public async Task DeleteBooking_BookingNotFound_ReturnsNotFound()
    {
        // Arrange
        SetupUser(1);

        // Act
        var result = await _controller.DeleteBooking(999);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task DeleteBooking_UnauthorizedUser_ReturnsForbid()
    {
        // Arrange
        SetupUser(2); // Different user
        var booking = new Booking
        {
            UserId = 1,
            VehicleId = 1,
            StartDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now.AddDays(3),
            TotalPrice = 100,
            Status = "Pending"
        };
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteBooking(booking.Id);

        // Assert
        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task CancelBooking_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        SetupUser(1);
        var booking = new Booking
        {
            UserId = 1,
            VehicleId = 1,
            StartDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now.AddDays(3),
            TotalPrice = 100,
            Status = "Pending"
        };
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.CancelBooking(booking.Id);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
        var updatedBooking = await _dbContext.Bookings.FindAsync(booking.Id);
        Assert.That(updatedBooking!.Status, Is.EqualTo("Cancelled"));
    }

    [Test]
    public async Task CancelBooking_AlreadyCancelled_ReturnsBadRequest()
    {
        // Arrange
        SetupUser(1);
        var booking = new Booking
        {
            UserId = 1,
            VehicleId = 1,
            StartDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now.AddDays(3),
            TotalPrice = 100,
            Status = "Cancelled"
        };
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.CancelBooking(booking.Id);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task GetMyBookings_ReturnsUserBookings()
    {
        // Arrange
        SetupUser(1);
        var booking1 = new Booking
        {
            UserId = 1,
            VehicleId = 1,
            StartDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now.AddDays(3),
            TotalPrice = 100,
            Status = "Pending",
            Vehicle = new Vehicle { Brand = "Toyota", Model = "Corolla", DailyPrice = 50, MongoImageIds = new List<string> { "image1" } },
            BookingExtras = new List<BookingExtra>()
        };
        var booking2 = new Booking
        {
            UserId = 2, // Different user
            VehicleId = 1,
            StartDate = DateTime.Now.AddDays(4),
            EndDate = DateTime.Now.AddDays(6),
            TotalPrice = 100,
            Status = "Confirmed",
            Vehicle = new Vehicle { Brand = "Toyota", Model = "Corolla", DailyPrice = 50, MongoImageIds = new List<string> { "image1" } },
        };
        _dbContext.Bookings.Add(booking1);
        _dbContext.Bookings.Add(booking2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetMyBookings();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var bookings = okResult!.Value as IEnumerable<MyBookingDto>;
        Assert.That(bookings, Is.Not.Null);
        Assert.That(bookings!.Count(), Is.EqualTo(1));
        Assert.That(bookings.First().Id, Is.EqualTo(booking1.Id));
    }

    [Test]
    public async Task UpdateBookingStatus_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        SetupUser(1);
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

        var booking = new Booking
        {
            UserId = 1,
            VehicleId = 1,
            StartDate = DateTime.Now.AddDays(1),
            EndDate = DateTime.Now.AddDays(3),
            TotalPrice = 100,
            Status = "Pending"
        };
        _dbContext.Bookings.Add(booking);
        await _dbContext.SaveChangesAsync();

        var dto = new UpdateBookingStatusDto { Status = "Confirmed" };

        // Act
        var result = await _controller.UpdateBookingStatus(booking.Id, dto);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
        var updatedBooking = await _dbContext.Bookings.FindAsync(booking.Id);
        Assert.That(updatedBooking!.Status, Is.EqualTo("Confirmed"));
    }

    [Test]
    public async Task UpdateBookingStatus_InvalidStatus_ReturnsBadRequest()
    {
        // Arrange
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

        var dto = new UpdateBookingStatusDto { Status = "Invalid" };

        // Act
        var result = await _controller.UpdateBookingStatus(1, dto);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }
}