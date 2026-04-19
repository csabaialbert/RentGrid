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
public class UserControllerTests
{
    private ApplicationDbContext _dbContext;
    private UserController _controller;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _controller = new UserController(_dbContext);

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
        var user1 = new User
        {
            Id = 1,
            FullName = "John Doe",
            Email = "john@example.com",
            PasswordHash = "hash1",
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };

        var user2 = new User
        {
            Id = 2,
            FullName = "Jane Admin",
            Email = "jane@example.com",
            PasswordHash = "hash2",
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        };

        var user3 = new User
        {
            Id = 3,
            FullName = "Bob Customer",
            Email = "bob@example.com",
            PasswordHash = "hash3",
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Users.AddRange(user1, user2, user3);
        _dbContext.SaveChanges();
    }

    private void SetupUser(int userId, string role = "Customer")
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        if (!string.IsNullOrEmpty(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Test]
    public async Task GetMe_ValidUser_ReturnsUserDto()
    {
        // Arrange
        SetupUser(1);

        // Act
        var result = await _controller.GetMe();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult!.Value, Is.InstanceOf<UserDto>());

        var userDto = okResult.Value as UserDto;
        Assert.That(userDto, Is.Not.Null);
        Assert.That(userDto!.Id, Is.EqualTo(1));
        Assert.That(userDto.Name, Is.EqualTo("John Doe"));
        Assert.That(userDto.Email, Is.EqualTo("john@example.com"));
        Assert.That(userDto.Role, Is.EqualTo("Customer"));
    }

    [Test]
    public async Task GetMe_AdminUser_ReturnsUserDto()
    {
        // Arrange
        SetupUser(2, "Admin");

        // Act
        var result = await _controller.GetMe();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var userDto = okResult!.Value as UserDto;

        Assert.That(userDto!.Id, Is.EqualTo(2));
        Assert.That(userDto.Name, Is.EqualTo("Jane Admin"));
        Assert.That(userDto.Email, Is.EqualTo("jane@example.com"));
        Assert.That(userDto.Role, Is.EqualTo("Admin"));
    }

    [Test]
    public async Task GetMe_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        SetupUser(999); // Non-existent user

        // Act
        var result = await _controller.GetMe();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task GetMe_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange - Set up controller with no user claims
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.GetMe();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task GetAllUsers_AdminUser_ReturnsAllUsers()
    {
        // Arrange
        SetupUser(2, "Admin");

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult!.Value, Is.InstanceOf<IEnumerable<UserDto>>());

        var users = okResult.Value as IEnumerable<UserDto>;
        Assert.That(users, Is.Not.Null);
        Assert.That(users!.Count(), Is.EqualTo(3));

        // Verify all users are returned
        var userIds = users.Select(u => u.Id).OrderBy(id => id).ToList();
        Assert.That(userIds, Is.EqualTo(new List<int> { 1, 2, 3 }));

        // Verify user details
        var john = users.First(u => u.Id == 1);
        Assert.That(john.Name, Is.EqualTo("John Doe"));
        Assert.That(john.Email, Is.EqualTo("john@example.com"));
        Assert.That(john.Role, Is.EqualTo("Customer"));

        var jane = users.First(u => u.Id == 2);
        Assert.That(jane.Name, Is.EqualTo("Jane Admin"));
        Assert.That(jane.Email, Is.EqualTo("jane@example.com"));
        Assert.That(jane.Role, Is.EqualTo("Admin"));
    }

    [Test]
    public async Task GetAllUsers_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange - Clear existing users
        _dbContext.Users.RemoveRange(_dbContext.Users);
        await _dbContext.SaveChangesAsync();
        SetupUser(1, "Admin");

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var users = okResult!.Value as IEnumerable<UserDto>;
        Assert.That(users, Is.Not.Null);
        Assert.That(users!.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetAllUsers_CustomerUser_ReturnsAllUsers()
    {
        // Arrange - Even non-admin users can access this endpoint in unit tests
        // since we don't test authorization middleware here
        SetupUser(1, "Customer");

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var users = okResult!.Value as IEnumerable<UserDto>;
        Assert.That(users, Is.Not.Null);
        Assert.That(users!.Count(), Is.EqualTo(3));
    }
}