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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

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
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        _userService = new UserService(_userRepository, new Mock<Microsoft.Extensions.Logging.ILogger<UserService>>().Object, memoryCache);

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

        var updateRequest = new UserUpdateDto { Name = "Alice Updated", Email = "alice.updated@example.com" };

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
        var updateRequest = new UserUpdateDto { Name = "Alice Updated", Email = "alice.updated@example.com" };

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

    [Test]
    public async Task GetAllAsync_NullQuery_ReturnsAllUsers()
    {
        // Arrange
        _context.Users.AddRange(
            new User { Id = 10, Name = "Alice", Email = "alice@example.com", PasswordHash = "hash", RoleId = 2, IsPending = false },
            new User { Id = 11, Name = "Bob", Email = "bob@example.com", PasswordHash = "hash", RoleId = 2, IsPending = false }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetAllAsync(null!);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public async Task GetAllAsync_WithFiltersAndPagination_ReturnsFilteredUsers()
    {
        // Arrange
        _context.Users.AddRange(
            new User { Id = 12, Name = "Alice Filtered", Email = "alicef@example.com", PasswordHash = "hash", RoleId = 2, IsPending = false },
            new User { Id = 13, Name = "Bob Filtered", Email = "bobf@example.com", PasswordHash = "hash", RoleId = 2, IsPending = true },
            new User { Id = 14, Name = "Charlie Filtered", Email = "charlief@example.com", PasswordHash = "hash", RoleId = 1, IsPending = false }
        );
        await _context.SaveChangesAsync();

        var query = new QueryUserDto
        {
            Search = "Al",
            RoleId = 2,
            IsPending = false,
            PageNumber = 1,
            PageSize = 5
        };

        // Act
        var result = await _userService.GetAllAsync(query);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Any(u => u.Name == "Alice Filtered"), Is.True);
        Assert.That(result.Any(u => u.Name == "Bob Filtered"), Is.False);
    }

    [Test]
    public async Task GetAllAsync_SearchMatchesEmail_Succeeds()
    {
        // Arrange
        _context.Users.AddRange(
            new User { Id = 15, Name = "Alice Email", Email = "alice.email@example.com", PasswordHash = "hash", RoleId = 2 }
        );
        await _context.SaveChangesAsync();

        var query = new QueryUserDto { Search = "alice.email" };

        // Act
        var result = await _userService.GetAllAsync(query);

        // Assert
        Assert.That(result.Any(u => u.Name == "Alice Email"), Is.True);
    }

    [Test]
    public async Task GetAllAsync_InvalidPageParameters_UsesDefaults()
    {
        // Arrange
        _context.Users.AddRange(
            new User { Id = 16, Name = "Alice Default", Email = "aliced@example.com", PasswordHash = "hash", RoleId = 2 }
        );
        await _context.SaveChangesAsync();

        var query = new QueryUserDto { PageNumber = 0, PageSize = 0 };

        // Act
        var result = await _userService.GetAllAsync(query);

        // Assert
        Assert.That(result.Any(u => u.Name == "Alice Default"), Is.True);
    }

    [Test]
    public async Task ApproveUserAsync_UserExists_ApprovesUser()
    {
        // Arrange
        var user = new User { Id = 17, Name = "Pending User", Email = "pending@example.com", PasswordHash = "hash", RoleId = 2, IsPending = true };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.ApproveUserAsync(17);

        // Assert
        Assert.That(result.IsPending, Is.False);
        var updated = await _context.Users.FindAsync(17);
        Assert.That(updated!.IsPending, Is.False);
    }

    [Test]
    public void ApproveUserAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _userService.ApproveUserAsync(99));
        Assert.That(ex.Message, Is.EqualTo("User with id 99 was not found."));
    }

    [Test]
    public async Task UpdateUserRoleAsync_ValidPromotion_Succeeds()
    {
        // Arrange: Seeding role 3 and user
        _context.Roles.Add(new Role { Id = 3, Name = UserRole.Chef });
        var user = new User { Id = 18, Name = "Staff", Email = "staff@example.com", PasswordHash = "hash", RoleId = 2, IsPending = false };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.UpdateUserRoleAsync(18, 3, 1); // target user id = 18, new role id = 3, current user id = 1

        // Assert
        Assert.That(result.RoleName, Is.EqualTo("Chef"));
    }

    [Test]
    public void UpdateUserRoleAsync_SelfPromotionDemotion_ThrowsBusinessRuleException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(async () => await _userService.UpdateUserRoleAsync(5, 1, 5));
        Assert.That(ex.Message, Is.EqualTo("You cannot change your own role."));
    }

    [Test]
    public void UpdateUserRoleAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () => await _userService.UpdateUserRoleAsync(99, 1, 1));
        Assert.That(ex.Message, Is.EqualTo("User with id 99 was not found."));
    }

    [Test]
    public async Task UpdateUserRoleAsync_PromotePendingToAdmin_ThrowsBusinessRuleException()
    {
        // Arrange
        var user = new User { Id = 19, Name = "Pending Staff", Email = "staff.pending@example.com", PasswordHash = "hash", RoleId = 2, IsPending = true };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(async () => await _userService.UpdateUserRoleAsync(19, 1, 1)); // 1 is Admin role ID
        Assert.That(ex.Message, Is.EqualTo("Only approved users can be promoted to Admin."));
    }

    [Test]
    public async Task UpdateUserRoleAsync_ConcurrencyDeletion_ThrowsNotFoundException()
    {
        // Arrange: Mocking the repository to return user initially, but null during role update
        var repoMock = new Mock<IUserRepository>();
        var user = new User { Id = 5, Name = "Staff", Email = "staff@example.com", PasswordHash = "hash", RoleId = 2, IsPending = false };
        repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);
        repoMock.Setup(r => r.UpdateRoleAsync(5, 3)).ReturnsAsync((User?)null);

        var userService = new UserService(repoMock.Object, new Mock<ILogger<UserService>>().Object, new MemoryCache(new MemoryCacheOptions()));

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(async () => await userService.UpdateUserRoleAsync(5, 3, 1));
        Assert.That(ex.Message, Is.EqualTo("User with id 5 was not found."));
    }

    [Test]
    public async Task MapUserToDto_UserWithNullRole_MapsRoleNameToCustomer()
    {
        // Arrange
        var user = new User { Id = 100, Name = "No Role User", Email = "norole@example.com", PasswordHash = "hash", Role = null, RoleId = 2 };
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.GetByIdAsync(100)).ReturnsAsync(user);

        var userService = new UserService(repoMock.Object, new Mock<ILogger<UserService>>().Object, new MemoryCache(new MemoryCacheOptions()));

        // Act
        var result = await userService.GetByIdAsync(100);

        // Assert
        Assert.That(result.RoleName, Is.EqualTo(string.Empty));
    }
}
