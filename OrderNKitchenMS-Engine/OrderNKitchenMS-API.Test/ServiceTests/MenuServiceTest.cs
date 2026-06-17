using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using OrderNKitchenMS_API.Data;
using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Repositories;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Services;
using OrderNKitchenMS_API.Services.Interfaces;

namespace OrderNKitchenMS_API.Test.ServiceTests;

[TestFixture]
public class MenuServiceTest
{
    private AppDbContext _context = null!;
    private IMenuItemRepository _menuItemRepository = null!;
    private ICategoryRepository _categoryRepository = null!;
    private Mock<IItemService> _itemServiceMock = null!;
    private IMenuService _menuService = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "OrderNKitchenDb")
            .Options;

        _context = new AppDbContext(options);
        _menuItemRepository = new MenuItemRepository(_context);
        _categoryRepository = new CategoryRepository(_context);
        _itemServiceMock = new Mock<IItemService>();

        var logger = new Mock<ILogger<MenuService>>().Object;
        _menuService = new MenuService(_menuItemRepository, _categoryRepository, _itemServiceMock.Object, logger);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task CreateAsync_PassTest()
    {
        // Arrange
        _context.Categories.Add(new Category { Id = 1, Name = "Appetizers" });
        await _context.SaveChangesAsync();

        var dto = new MenuItemCreateDto
        {
            Name = "Spring Rolls",
            Description = "Crispy rolls",
            Price = 6.5m,
            CategoryId = 1,
            ImageUrl = "http://springrolls.jpg",
            PreparationTime = 15
        };

        // Act
        var result = await _menuService.CreateAsync(dto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Spring Rolls"));
        Assert.That(result.Price, Is.EqualTo(6.5m));
    }

    [Test]
    public void CreateAsync_FailTest_CategoryNotFound_ThrowsNotFoundException()
    {
        var dto = new MenuItemCreateDto
        {
            Name = "Spring Rolls",
            Description = "Crispy rolls",
            Price = 6.5m,
            CategoryId = 999, // Category doesn't exist
            ImageUrl = "http://springrolls.jpg",
            PreparationTime = 15
        };

        Assert.ThrowsAsync<NotFoundException>(async () => await _menuService.CreateAsync(dto));
    }

    [Test]
    public async Task GetByIdAsync_PassTest()
    {
        // Arrange
        _context.Categories.Add(new Category { Id = 1, Name = "Appetizers" });
        _context.MenuItems.Add(new MenuItem { Id = 5, Name = "Dumplings", Price = 8m, CategoryId = 1, PreparationTime = 10 });
        await _context.SaveChangesAsync();

        // Act
        var result = await _menuService.GetByIdAsync(5);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(5));
        Assert.That(result.Name, Is.EqualTo("Dumplings"));
    }

    [Test]
    public void GetByIdAsync_FailTest_NotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _menuService.GetByIdAsync(999));
    }

    [Test]
    public async Task ToggleAvailabilityAsync_PassTest_EnableAvailableItem()
    {
        // Arrange
        _context.Categories.Add(new Category { Id = 1, Name = "Appetizers" });
        _context.MenuItems.Add(new MenuItem { Id = 2, Name = "Dumplings", Price = 8m, CategoryId = 1, PreparationTime = 10, IsAvailable = false });
        await _context.SaveChangesAsync();

        _itemServiceMock.Setup(s => s.CanPrepareMenuItemAsync(2, 1)).ReturnsAsync(true);

        // Act
        var success = await _menuService.ToggleAvailabilityAsync(2, true);

        // Assert
        Assert.That(success, Is.True);
        var updated = await _context.MenuItems.FindAsync(2);
        Assert.That(updated!.IsAvailable, Is.True);
    }

    [Test]
    public async Task ToggleAvailabilityAsync_FailTest_CannotEnableZeroStock_ThrowsBusinessRuleException()
    {
        // Arrange
        _context.Categories.Add(new Category { Id = 1, Name = "Appetizers" });
        _context.MenuItems.Add(new MenuItem { Id = 2, Name = "Dumplings", Price = 8m, CategoryId = 1, PreparationTime = 10, IsAvailable = false });
        await _context.SaveChangesAsync();

        _itemServiceMock.Setup(s => s.CanPrepareMenuItemAsync(2, 1)).ReturnsAsync(false); // Can't prepare (insufficient stock)

        // Act & Assert
        Assert.ThrowsAsync<BusinessRuleException>(async () => await _menuService.ToggleAvailabilityAsync(2, true));
    }

    [Test]
    public async Task CreateAsync_DuplicateName_ThrowsConflictException()
    {
        // Arrange
        _context.Categories.Add(new Category { Id = 1, Name = "Appetizers" });
        _context.MenuItems.Add(new MenuItem { Id = 10, Name = "Spring Rolls", Price = 6.5m, CategoryId = 1, PreparationTime = 15 });
        await _context.SaveChangesAsync();

        var dto = new MenuItemCreateDto
        {
            Name = "Spring Rolls", // Exact duplicate
            Description = "Another crispy rolls",
            Price = 7m,
            CategoryId = 1,
            ImageUrl = "http://springrolls2.jpg",
            PreparationTime = 15
        };

        // Act & Assert
        Assert.ThrowsAsync<ConflictException>(async () => await _menuService.CreateAsync(dto));
    }

    [Test]
    public async Task UpdateAsync_DuplicateName_ThrowsConflictException()
    {
        // Arrange
        _context.Categories.Add(new Category { Id = 1, Name = "Appetizers" });
        _context.MenuItems.Add(new MenuItem { Id = 10, Name = "Spring Rolls", Price = 6.5m, CategoryId = 1, PreparationTime = 15 });
        _context.MenuItems.Add(new MenuItem { Id = 11, Name = "Samosa", Price = 5m, CategoryId = 1, PreparationTime = 10 });
        await _context.SaveChangesAsync();

        var dto = new MenuItemUpdateDto
        {
            Name = "Spring Rolls", // Duplicate of ID 10
            Description = "Tasty triangle",
            Price = 5.5m,
            CategoryId = 1,
            ImageUrl = "http://samosa.jpg",
            PreparationTime = 10
        };

        // Act & Assert
        Assert.ThrowsAsync<ConflictException>(async () => await _menuService.UpdateAsync(11, dto));
    }
}
