using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using RentGrid.Api.Controllers;
using RentGrid.Api.Data;
using RentGrid.Api.Dtos;
using RentGrid.Api.Models;
using RentGrid.Api.Services;

namespace RentGrid.Api.Tests;

[TestFixture]
public class AuthControllerTests
{
    private ApplicationDbContext _dbContext;
    private Mock<IAuthService> _mockAuthService;
    private AuthController _controller;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _mockAuthService = new Mock<IAuthService>();
        _controller = new AuthController(_dbContext, _mockAuthService.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    public async Task Register_ValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            FullName = "Test User",
            Email = "test@example.com",
            Password = "password123"
        };

        _mockAuthService.Setup(s => s.HashPassword(It.IsAny<string>())).Returns("hashedpassword");
        _mockAuthService.Setup(s => s.GenerateTokenAsync(It.IsAny<User>())).ReturnsAsync("token123");

        // Act
        var result = await _controller.Register(request);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<CreatedResult>());
        var createdResult = result.Result as CreatedResult;
        Assert.That(createdResult!.Value, Is.InstanceOf<AuthResponseDto>());
        var response = createdResult.Value as AuthResponseDto;
        Assert.That(response!.Token, Is.EqualTo("token123"));
    }

    [Test]
    public async Task Register_MissingFields_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            FullName = "",
            Email = "test@example.com",
            Password = "password123"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Register_EmailAlreadyExists_ReturnsBadRequest()
    {
        // Arrange
        var existingUser = new User { Id = 1, FullName = "Existing User", Email = "existing@example.com", PasswordHash = "hash" };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var request = new RegisterRequestDto
        {
            FullName = "Test User",
            Email = "existing@example.com",
            Password = "password123"
        };

        // Act
        var result = await _controller.Register(request);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Login_ValidCredentials_ReturnsOkResult()
    {
        // Arrange
        var user = new User { Id = 1, FullName = "Test User", Email = "test@example.com", PasswordHash = "hashedpassword" };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var request = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "password123"
        };

        _mockAuthService.Setup(s => s.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _mockAuthService.Setup(s => s.GenerateTokenAsync(It.IsAny<User>())).ReturnsAsync("token123");

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult!.Value, Is.InstanceOf<AuthResponseDto>());
        var response = okResult.Value as AuthResponseDto;
        Assert.That(response!.Token, Is.EqualTo("token123"));
    }

    [Test]
    public async Task Login_MissingFields_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "",
            Password = "password123"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequestDto
        {
            Email = "nonexistent@example.com",
            Password = "wrongpassword"
        };

        // Act
        var result = await _controller.Login(request);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<UnauthorizedObjectResult>());
    }
}