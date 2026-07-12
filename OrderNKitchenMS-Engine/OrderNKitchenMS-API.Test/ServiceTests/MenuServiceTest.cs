using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
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
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _menuItemRepository = new MenuItemRepository(_context);
        _categoryRepository = new CategoryRepository(_context);
        _itemServiceMock = new Mock<IItemService>();

        var logger = new Mock<ILogger<MenuService>>().Object;
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        _menuService = new MenuService(_menuItemRepository, _categoryRepository, _itemServiceMock.Object, logger, _context, memoryCache);
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

    [Test]
    public async Task GetAllAsync_AppliesAllFiltersAndPagingAndCaching()
    {
        // Arrange
        _context.Categories.Add(new Category { Id = 1, Name = "Category A" });
        _context.Categories.Add(new Category { Id = 2, Name = "Category B" });
        _context.MenuItems.AddRange(
            new MenuItem { Id = 1, Name = "Apple Pie", CategoryId = 1, Price = 5m, IsAvailable = true, PreparationTime = 10 },
            new MenuItem { Id = 2, Name = "Banana Split", CategoryId = 1, Price = 10m, IsAvailable = true, PreparationTime = 15 },
            new MenuItem { Id = 3, Name = "Chocolate Cake", CategoryId = 2, Price = 15m, IsAvailable = false, PreparationTime = 20 }
        );
        await _context.SaveChangesAsync();

        // Query 1: Name match
        var queryName = new QueryMenuItemDto { Name = "Apple" };
        var resultName = await _menuService.GetAllAsync(queryName);
        Assert.That(resultName.Count(), Is.EqualTo(1));
        Assert.That(resultName.First().Name, Is.EqualTo("Apple Pie"));

        // Query 2: CategoryId match
        var queryCat = new QueryMenuItemDto { CategoryId = 1 };
        var resultCat = await _menuService.GetAllAsync(queryCat);
        Assert.That(resultCat.Count(), Is.EqualTo(2));

        // Query 3: MinPrice
        var queryMinPrice = new QueryMenuItemDto { MinPrice = 9m };
        var resultMinPrice = await _menuService.GetAllAsync(queryMinPrice);
        Assert.That(resultMinPrice.Count(), Is.EqualTo(2));

        // Query 4: MaxPrice
        var queryMaxPrice = new QueryMenuItemDto { MaxPrice = 11m };
        var resultMaxPrice = await _menuService.GetAllAsync(queryMaxPrice);
        Assert.That(resultMaxPrice.Count(), Is.EqualTo(2));

        // Query 5: IsAvailable
        var queryAvail = new QueryMenuItemDto { IsAvailable = false };
        var resultAvail = await _menuService.GetAllAsync(queryAvail);
        Assert.That(resultAvail.Count(), Is.EqualTo(1));
        Assert.That(resultAvail.First().Name, Is.EqualTo("Chocolate Cake"));

        // Query 6: MaxPreparationTime
        var queryTime = new QueryMenuItemDto { MaxPreparationTime = 12 };
        var resultTime = await _menuService.GetAllAsync(queryTime);
        Assert.That(resultTime.Count(), Is.EqualTo(1));
        Assert.That(resultTime.First().Name, Is.EqualTo("Apple Pie"));

        // Query 7: Pagination edge cases (PageNumber = 0, PageSize = 0)
        var queryPaging = new QueryMenuItemDto { PageNumber = 0, PageSize = 0 };
        var resultPaging = await _menuService.GetAllAsync(queryPaging);
        Assert.That(resultPaging.Count(), Is.EqualTo(3)); // PageSize defaults to 10

        // Query 8: Name is empty string
        var queryNameEmpty = new QueryMenuItemDto { Name = "" };
        var resultNameEmpty = await _menuService.GetAllAsync(queryNameEmpty);
        Assert.That(resultNameEmpty.Count(), Is.EqualTo(3));

        // Test Caching: Clear DB and call again to verify it loads from cache
        _context.MenuItems.RemoveRange(_context.MenuItems);
        await _context.SaveChangesAsync();

        var queryCached = new QueryMenuItemDto();
        var resultCached = await _menuService.GetAllAsync(queryCached);
        Assert.That(resultCached.Count(), Is.EqualTo(3)); // Still returns 3 from cache
    }

    [Test]
    public async Task UpdateAsync_PassTest()
    {
        // Arrange
        _context.Categories.Add(new Category { Id = 1, Name = "Category A" });
        _context.MenuItems.Add(new MenuItem { Id = 15, Name = "Old Name", CategoryId = 1, Price = 5m, PreparationTime = 10 });
        await _context.SaveChangesAsync();

        var dto = new MenuItemUpdateDto
        {
            Name = "New Name",
            Description = "Updated desc",
            Price = 6m,
            CategoryId = 1,
            ImageUrl = "http://new.jpg",
            PreparationTime = 12,
            IsAvailable = true
        };

        // Act
        var result = await _menuService.UpdateAsync(15, dto);

        // Assert
        Assert.That(result.Name, Is.EqualTo("New Name"));
        Assert.That(result.PreparationTime, Is.EqualTo(12));
        var updated = await _context.MenuItems.FindAsync(15);
        Assert.That(updated!.Name, Is.EqualTo("New Name"));
    }

    [Test]
    public void UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        _context.Categories.Add(new Category { Id = 1, Name = "Category A" });
        _context.SaveChanges();

        var dto = new MenuItemUpdateDto
        {
            Name = "New Name",
            CategoryId = 1,
            Price = 6m,
            PreparationTime = 12
        };

        Assert.ThrowsAsync<NotFoundException>(async () => await _menuService.UpdateAsync(999, dto));
    }

    [Test]
    public async Task ToggleAvailabilityAsync_DisableAvailableItem_Succeeds()
    {
        // Arrange
        _context.Categories.Add(new Category { Id = 1, Name = "Category A" });
        _context.MenuItems.Add(new MenuItem { Id = 20, Name = "Dumplings", Price = 8m, CategoryId = 1, PreparationTime = 10, IsAvailable = true });
        await _context.SaveChangesAsync();

        // Act
        var success = await _menuService.ToggleAvailabilityAsync(20, false);

        // Assert
        Assert.That(success, Is.True);
        var updated = await _context.MenuItems.FindAsync(20);
        Assert.That(updated!.IsAvailable, Is.False);
        Assert.That(updated.IsManuallyDisabled, Is.True);
    }

    [Test]
    public void ToggleAvailabilityAsync_NotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _menuService.ToggleAvailabilityAsync(999, true));
    }

    [Test]
    public async Task DeleteAsync_PassTest()
    {
        // Arrange
        _context.Categories.Add(new Category { Id = 1, Name = "Category A" });
        _context.MenuItems.Add(new MenuItem { Id = 25, Name = "Dumplings", Price = 8m, CategoryId = 1, PreparationTime = 10 });
        await _context.SaveChangesAsync();

        // Act
        var success = await _menuService.DeleteAsync(25);

        // Assert
        Assert.That(success, Is.True);
        var deleted = await _context.MenuItems.FindAsync(25);
        Assert.That(deleted!.IsDeleted, Is.True);
    }

    [Test]
    public void DeleteAsync_NotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _menuService.DeleteAsync(999));
    }

    [Test]
    public void GetAllAsync_WithNullQuery_ThrowsNullReferenceException()
    {
        Assert.ThrowsAsync<NullReferenceException>(async () => await _menuService.GetAllAsync(null!));
    }
}
