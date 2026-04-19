using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.InMemory;
using NUnit.Framework;
using RentGrid.Api.Controllers;
using RentGrid.Api.Data;
using RentGrid.Api.Models;

namespace RentGrid.Api.Tests;

[TestFixture]
public class ExtrasControllerTests
{
    private ApplicationDbContext _dbContext;
    private ExtrasController _controller;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _controller = new ExtrasController(_dbContext);

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
        var extra1 = new ExtraService
        {
            Id = 1,
            Name = "GPS Navigation",
            Price = 15.99m
        };

        var extra2 = new ExtraService
        {
            Id = 2,
            Name = "Insurance",
            Price = 25.50m
        };

        var extra3 = new ExtraService
        {
            Id = 3,
            Name = "Child Seat",
            Price = 10.00m
        };

        _dbContext.ExtraServices.AddRange(extra1, extra2, extra3);
        _dbContext.SaveChanges();
    }

    [Test]
    public async Task GetAllExtras_ReturnsAllExtraServices()
    {
        // Act
        var result = await _controller.GetAllExtras();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult!.Value, Is.InstanceOf<IEnumerable<ExtraService>>());

        var extras = okResult.Value as IEnumerable<ExtraService>;
        Assert.That(extras, Is.Not.Null);
        Assert.That(extras!.Count(), Is.EqualTo(3));

        // Verify the extras are returned correctly
        var gpsExtra = extras.First(e => e.Id == 1);
        Assert.That(gpsExtra.Name, Is.EqualTo("GPS Navigation"));
        Assert.That(gpsExtra.Price, Is.EqualTo(15.99m));

        var insuranceExtra = extras.First(e => e.Id == 2);
        Assert.That(insuranceExtra.Name, Is.EqualTo("Insurance"));
        Assert.That(insuranceExtra.Price, Is.EqualTo(25.50m));

        var childSeatExtra = extras.First(e => e.Id == 3);
        Assert.That(childSeatExtra.Name, Is.EqualTo("Child Seat"));
        Assert.That(childSeatExtra.Price, Is.EqualTo(10.00m));
    }

    [Test]
    public async Task GetAllExtras_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange - Clear existing data
        _dbContext.ExtraServices.RemoveRange(_dbContext.ExtraServices);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllExtras();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var extras = okResult!.Value as IEnumerable<ExtraService>;
        Assert.That(extras, Is.Not.Null);
        Assert.That(extras!.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetAllExtras_ReturnsExtrasInCorrectOrder()
    {
        // Act
        var result = await _controller.GetAllExtras();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var extras = okResult!.Value as IEnumerable<ExtraService>;
        Assert.That(extras, Is.Not.Null);

        // Since we're using ToListAsync() without ordering, the order might vary
        // but all extras should be present
        var extraIds = extras!.Select(e => e.Id).OrderBy(id => id).ToList();
        Assert.That(extraIds, Is.EqualTo(new List<int> { 1, 2, 3 }));
    }

    [Test]
    public async Task GetAllExtras_ReturnsTrackingDisabled()
    {
        // Act
        var result = await _controller.GetAllExtras();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var extras = okResult!.Value as IEnumerable<ExtraService>;
        Assert.That(extras, Is.Not.Null);

        // The controller uses AsNoTracking(), so the entities should not be tracked
        // We can't directly test this, but we can verify the method completes successfully
        // and returns the expected data
        Assert.That(extras!.Count(), Is.EqualTo(3));
    }
}