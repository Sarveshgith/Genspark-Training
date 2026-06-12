using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
using Moq;

namespace OrderNKitchenMS_API.Test.ServiceTests;

[TestFixture]
public class UserServiceTest
{
    private AppDbContext _context = null!;
    private IUserRepository _userRepository = null!;
    private IUserService _userService = null!;

    [SetUp]
    public void SetUp()
    {
        // ARRANGE: Setup in-memory DbContext and service instance before each test
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _userRepository = new UserRepository(_context);
        _userService = new UserService(_userRepository, new Mock<Microsoft.Extensions.Logging.ILogger<UserService>>().Object);

        // Seed roles required by foreign keys
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

    [Test]
    public async Task GetAllAsync_Success_ReturnsUserDTOs()
    {
        // Arrange: Add users to the database
        _context.Users.AddRange(
            new User { Id = 1, Name = "Alice", Email = "alice@example.com", PasswordHash = "hash", RoleId = 2 },
            new User { Id = 2, Name = "Bob", Email = "bob@example.com", PasswordHash = "hash", RoleId = 2 }
        );
        _context.SaveChanges();

        var query = new QueryUserDto { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _userService.GetAllAsync(query);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.First().Name, Is.EqualTo("Alice"));
        Assert.That(result.Last().Name, Is.EqualTo("Bob"));
    }

    [Test]
    public async Task GetAllRolesAsync_Success_ReturnsRoleDTOs()
    {
        // Act
        var result = await _userService.GetAllRolesAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.First().Name, Is.EqualTo("Admin"));
    }

    [Test]
    public async Task GetByIdAsync_UserExists_ReturnsUserDTO()
    {
        // Arrange: Save a user to the database
        var user = new User { Id = 1, Name = "Alice", Email = "alice@example.com", PasswordHash = "hash", RoleId = 2 };
        _context.Users.Add(user);
        _context.SaveChanges();

        // Act
        var result = await _userService.GetByIdAsync(1);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(user.Id));
        Assert.That(result.Name, Is.EqualTo(user.Name));
        Assert.That(result.Email, Is.EqualTo(user.Email));
    }

    [Test]
    public void GetByIdAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _userService.GetByIdAsync(99));
        Assert.That(ex.Message, Is.EqualTo("User with id 99 was not found."));
    }

    [Test]
    public async Task UpdateAsync_ValidUser_ReturnsUpdatedUserDTO()
    {
        // Arrange: Seed user in database
        var existingUser = new User { Id = 1, Name = "Alice", Email = "alice@example.com", PasswordHash = "hash", RoleId = 2 };
        _context.Users.Add(existingUser);
        _context.SaveChanges();

        var updateRequest = new UserUpdateDto { Name = "Alice Updated", Email = "alice.updated@example.com", RoleId = 2 };

        // Act
        var result = await _userService.UpdateAsync(1, updateRequest);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo(updateRequest.Name));
        Assert.That(result.Email, Is.EqualTo(updateRequest.Email));
    }

    [Test]
    public void UpdateAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var updateRequest = new UserUpdateDto { Name = "Alice Updated", Email = "alice.updated@example.com", RoleId = 2 };

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _userService.UpdateAsync(99, updateRequest));
        Assert.That(ex.Message, Is.EqualTo("User with id 99 was not found."));
    }

    [Test]
    public async Task ChangePasswordAsync_UserExists_ReturnsTrue()
    {
        // Arrange: Seed user
        var user = new User { Id = 1, Name = "Alice", Email = "alice@example.com", PasswordHash = "hash", RoleId = 2 };
        _context.Users.Add(user);
        _context.SaveChanges();

        // Act
        var result = await _userService.ChangePasswordAsync(1, "newhash");

        // Assert
        Assert.That(result, Is.True);
        Assert.That(user.PasswordHash, Is.EqualTo("newhash"));
    }

    [Test]
    public void ChangePasswordAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _userService.ChangePasswordAsync(99, "newhash"));
        Assert.That(ex.Message, Is.EqualTo("User with id 99 was not found."));
    }

    [Test]
    public async Task DeleteAsync_UserExists_ReturnsTrue()
    {
        // Arrange: Seed user
        var user = new User { Id = 1, Name = "Alice", Email = "alice@example.com", PasswordHash = "hash", RoleId = 2 };
        _context.Users.Add(user);
        _context.SaveChanges();

        // Act
        var result = await _userService.DeleteAsync(1);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(user.IsDeleted, Is.True);
    }

    [Test]
    public void DeleteAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _userService.DeleteAsync(99));
        Assert.That(ex.Message, Is.EqualTo("User with id 99 was not found."));
    }
}
