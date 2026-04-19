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
public class DashboardControllerTests
{
    private ApplicationDbContext _dbContext;
    private DashboardController _controller;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _controller = new DashboardController(_dbContext);

        // Seed data
        SeedDatabase();

        // Setup admin user
        SetupAdminUser();
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    private void SeedDatabase()
    {
        // Create users
        var user1 = new User { Id = 1, FullName = "User One", Email = "user1@example.com", PasswordHash = "hash1" };
        var user2 = new User { Id = 2, FullName = "User Two", Email = "user2@example.com", PasswordHash = "hash2" };
        var user3 = new User { Id = 3, FullName = "User Three", Email = "user3@example.com", PasswordHash = "hash3" };

        // Create vehicles
        var vehicle1 = new Vehicle { Id = 1, Brand = "Toyota", Model = "Corolla", DailyPrice = 50, IsAvailable = true };
        var vehicle2 = new Vehicle { Id = 2, Brand = "Honda", Model = "Civic", DailyPrice = 45, IsAvailable = true };
        var vehicle3 = new Vehicle { Id = 3, Brand = "Ford", Model = "Focus", DailyPrice = 40, IsAvailable = true };

        // Create bookings with different dates and vehicles
        var now = DateTime.UtcNow;
        var pastBooking1 = new Booking
        {
            Id = 1,
            UserId = 1,
            VehicleId = 1,
            StartDate = now.AddDays(-10),
            EndDate = now.AddDays(-8),
            TotalPrice = 100,
            Status = "Confirmed"
        };
        var pastBooking2 = new Booking
        {
            Id = 2,
            UserId = 2,
            VehicleId = 1, // Same vehicle, should be most popular
            StartDate = now.AddDays(-5),
            EndDate = now.AddDays(-3),
            TotalPrice = 100,
            Status = "Confirmed"
        };
        var activeBooking = new Booking
        {
            Id = 3,
            UserId = 1,
            VehicleId = 2,
            StartDate = now.AddDays(-1),
            EndDate = now.AddDays(2), // Active booking
            TotalPrice = 135,
            Status = "Confirmed"
        };
        var futureBooking = new Booking
        {
            Id = 4,
            UserId = 3,
            VehicleId = 3,
            StartDate = now.AddDays(5),
            EndDate = now.AddDays(7),
            TotalPrice = 120,
            Status = "Pending"
        };
        var anotherBookingForPopular = new Booking
        {
            Id = 5,
            UserId = 2,
            VehicleId = 1, // Third booking for vehicle 1
            StartDate = now.AddDays(10),
            EndDate = now.AddDays(12),
            TotalPrice = 100,
            Status = "Confirmed"
        };

        _dbContext.Users.AddRange(user1, user2, user3);
        _dbContext.Vehicles.AddRange(vehicle1, vehicle2, vehicle3);
        _dbContext.Bookings.AddRange(pastBooking1, pastBooking2, activeBooking, futureBooking, anotherBookingForPopular);
        _dbContext.SaveChanges();
    }

    private void SetupAdminUser()
    {
        var claims = new List<Claim>
        {
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

    [Test]
    public async Task GetStats_ReturnsCorrectStatistics()
    {
        // Act
        var result = await _controller.GetStats();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult!.Value, Is.InstanceOf<DashboardStatsDto>());

        var stats = okResult.Value as DashboardStatsDto;
        Assert.That(stats, Is.Not.Null);

        // Check total revenue: 100 + 100 + 135 + 120 + 100 = 555
        Assert.That(stats!.TotalRevenue, Is.EqualTo(555m));

        // Check user count: 3 users
        Assert.That(stats.RegisteredUserCount, Is.EqualTo(3));

        // Check active booking count: 1 active booking
        Assert.That(stats.ActiveBookingCount, Is.EqualTo(1));

        // Check most popular vehicle: Vehicle 1 with 3 bookings
        Assert.That(stats.MostPopularVehicle, Is.Not.Null);
        Assert.That(stats.MostPopularVehicle!.VehicleId, Is.EqualTo(1));
        Assert.That(stats.MostPopularVehicle.Brand, Is.EqualTo("Toyota"));
        Assert.That(stats.MostPopularVehicle.Model, Is.EqualTo("Corolla"));
        Assert.That(stats.MostPopularVehicle.BookingCount, Is.EqualTo(3));
    }

    [Test]
    public async Task GetStats_NoBookings_ReturnsZeroRevenueAndActiveBookings()
    {
        // Arrange - Clear existing data and add only users
        _dbContext.Bookings.RemoveRange(_dbContext.Bookings);
        _dbContext.Vehicles.RemoveRange(_dbContext.Vehicles);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetStats();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var stats = okResult!.Value as DashboardStatsDto;

        Assert.That(stats!.TotalRevenue, Is.EqualTo(0m));
        Assert.That(stats.RegisteredUserCount, Is.EqualTo(3));
        Assert.That(stats.ActiveBookingCount, Is.EqualTo(0));
        Assert.That(stats.MostPopularVehicle, Is.Null);
    }

    [Test]
    public async Task GetStats_NoUsers_ReturnsZeroUserCount()
    {
        // Arrange - Clear users
        _dbContext.Users.RemoveRange(_dbContext.Users);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetStats();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var stats = okResult!.Value as DashboardStatsDto;

        Assert.That(stats!.RegisteredUserCount, Is.EqualTo(0));
    }

    [Test]
    public async Task GetStats_SingleBookingForEachVehicle_ReturnsCorrectPopularVehicle()
    {
        // Arrange - Clear existing bookings and add one booking per vehicle
        _dbContext.Bookings.RemoveRange(_dbContext.Bookings);
        await _dbContext.SaveChangesAsync();

        var now = DateTime.UtcNow;
        var booking1 = new Booking
        {
            UserId = 1,
            VehicleId = 1,
            StartDate = now.AddDays(1),
            EndDate = now.AddDays(3),
            TotalPrice = 100,
            Status = "Confirmed"
        };
        var booking2 = new Booking
        {
            UserId = 2,
            VehicleId = 2,
            StartDate = now.AddDays(1),
            EndDate = now.AddDays(3),
            TotalPrice = 90,
            Status = "Confirmed"
        };
        var booking3 = new Booking
        {
            UserId = 3,
            VehicleId = 3,
            StartDate = now.AddDays(1),
            EndDate = now.AddDays(3),
            TotalPrice = 80,
            Status = "Confirmed"
        };

        _dbContext.Bookings.AddRange(booking1, booking2, booking3);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetStats();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var stats = okResult!.Value as DashboardStatsDto;

        // All vehicles have 1 booking each, so any could be returned as most popular
        // The query uses FirstOrDefault after OrderByDescending, so it will return the first one
        Assert.That(stats!.MostPopularVehicle, Is.Not.Null);
        Assert.That(stats.MostPopularVehicle!.BookingCount, Is.EqualTo(1));
    }
}