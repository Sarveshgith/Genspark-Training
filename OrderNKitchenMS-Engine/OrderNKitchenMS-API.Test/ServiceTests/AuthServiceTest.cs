using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using OrderNKitchenMS_API.Data;
using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Models.Enums;
using OrderNKitchenMS_API.Repositories;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Services;
using OrderNKitchenMS_API.Services.Interfaces;

namespace OrderNKitchenMS_API.Test.ServiceTests;

[TestFixture]
public class AuthServiceTest
{
    private AppDbContext _context = null!;
    private IUserRepository _userRepository = null!;
    private Mock<ITokenService> _tokenServiceMock = null!;
    private IAuthService _authService = null!;

    [SetUp]
    public void SetUp()
    {
        // ARRANGE: Setup in-memory DbContext, repository, Mock token service, and AuthService instance before each test
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _userRepository = new UserRepository(_context);
        _tokenServiceMock = new Mock<ITokenService>();
        _authService = new AuthService(_userRepository, _tokenServiceMock.Object, new Mock<Microsoft.Extensions.Logging.ILogger<AuthService>>().Object);

        // Seed roles required by database relationships
        _context.Roles.AddRange(
            new Role { Id = 1, Name = UserRole.Admin },
            new Role { Id = 2, Name = UserRole.Customer }
        );
        _context.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region RegisterAsync Tests

    [Test]
    public async Task RegisterAsync_ValidUser_ReturnsUserDTO()
    {
        // Arrange
        var registerDto = new UserRegisterDto
        {
            Name = "John Doe",
            Email = "john@example.com",
            Password = "Password@123",
            RoleId = 2,
            PhoneNumber = "9876543210"
        };

        _tokenServiceMock.Setup(t => t.HashPassword(registerDto.Password)).Returns("hashed_Password@123");

        // Act
        var result = await _authService.RegisterAsync(registerDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("John Doe"));
        Assert.That(result.Email, Is.EqualTo("john@example.com"));
        Assert.That(result.RoleName, Is.EqualTo("Customer"));

        // Confirm user is saved in the actual database
        var savedUser = _context.Users.FirstOrDefault(u => u.Email == "john@example.com");
        Assert.That(savedUser, Is.Not.Null);
        Assert.That(savedUser.PasswordHash, Is.EqualTo("hashed_Password@123"));
    }

    [Test]
    public void RegisterAsync_EmailAlreadyExists_ThrowsConflictException()
    {
        // Arrange: Seed existing user in database
        _context.Users.Add(new User { Id = 1, Name = "Existing", Email = "john@example.com", PasswordHash = "hash", RoleId = 2 });
        _context.SaveChanges();

        var registerDto = new UserRegisterDto
        {
            Name = "John Doe",
            Email = "john@example.com", // Duplicate email
            Password = "Password@123",
            RoleId = 2
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<ConflictException>(async () => await _authService.RegisterAsync(registerDto));
        Assert.That(ex.Message, Is.EqualTo("Email is already registered."));
    }

    #endregion

    #region LoginAsync Tests

    [Test]
    public async Task LoginAsync_ValidCredentials_ReturnsLoginResponseDTO()
    {
        // Arrange: Seed user in database
        _context.Users.Add(new User { Id = 1, Name = "John Doe", Email = "john@example.com", PasswordHash = "hashed_Password@123", RoleId = 2 });
        _context.SaveChanges();

        var loginDto = new UserLoginDto { Email = "john@example.com", Password = "Password@123" };

        _tokenServiceMock.Setup(t => t.VerifyPassword(loginDto.Password, "hashed_Password@123")).Returns(true);
        _tokenServiceMock.Setup(t => t.CreateJwtToken(It.IsAny<User>())).Returns("jwt_token");
        _tokenServiceMock.Setup(t => t.CreateRefreshJwtToken(It.IsAny<User>())).Returns("refresh_token");

        // Act
        var result = await _authService.LoginAsync(loginDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Token, Is.EqualTo("jwt_token"));
        Assert.That(result.RefreshToken, Is.EqualTo("refresh_token"));
        Assert.That(result.User.Name, Is.EqualTo("John Doe"));
    }

    [Test]
    public void LoginAsync_InvalidCredentials_ThrowsArgumentException()
    {
        // Arrange: Seed user
        _context.Users.Add(new User { Id = 1, Name = "John Doe", Email = "john@example.com", PasswordHash = "hashed_Password@123", RoleId = 2 });
        _context.SaveChanges();

        var loginDto = new UserLoginDto { Email = "john@example.com", Password = "WrongPassword" };

        _tokenServiceMock.Setup(t => t.VerifyPassword(loginDto.Password, "hashed_Password@123")).Returns(false);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ArgumentException>(async () => await _authService.LoginAsync(loginDto));
        Assert.That(ex.Message, Is.EqualTo("Invalid email or password."));
    }

    #endregion

    #region RefreshTokenAsync Tests

    [Test]
    public async Task RefreshTokenAsync_ValidRefreshToken_ReturnsLoginResponseDTO()
    {
        // Arrange: Seed user in database
        var user = new User { Id = 1, Name = "John Doe", Email = "john@example.com", PasswordHash = "hashed_Password@123", RoleId = 2 };
        _context.Users.Add(user);
        _context.SaveChanges();

        var refreshDto = new TokenRefreshDto { RefreshToken = "valid_refresh_token" };

        var claims = new List<System.Security.Claims.Claim> { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "1") };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuthType");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        _tokenServiceMock.Setup(t => t.ValidateRefreshToken("valid_refresh_token")).Returns(principal);
        _tokenServiceMock.Setup(t => t.CreateJwtToken(It.IsAny<User>())).Returns("new_jwt_token");
        _tokenServiceMock.Setup(t => t.CreateRefreshJwtToken(It.IsAny<User>())).Returns("new_refresh_token");

        // Act
        var result = await _authService.RefreshTokenAsync(refreshDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Token, Is.EqualTo("new_jwt_token"));
        Assert.That(result.RefreshToken, Is.EqualTo("new_refresh_token"));
        Assert.That(result.User.Name, Is.EqualTo("John Doe"));
    }

    [Test]
    public void RefreshTokenAsync_UserNotFound_ThrowsUnauthorizedException()
    {
        // Arrange
        var refreshDto = new TokenRefreshDto { RefreshToken = "valid_refresh_token" };

        var claims = new List<System.Security.Claims.Claim> { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "99") };
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuthType");
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);

        _tokenServiceMock.Setup(t => t.ValidateRefreshToken("valid_refresh_token")).Returns(principal);

        // Act & Assert
        var ex = Assert.ThrowsAsync<UnauthorizedException>(async () => await _authService.RefreshTokenAsync(refreshDto));
        Assert.That(ex.Message, Is.EqualTo("User associated with this token was not found."));
    }

    #endregion
}
