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
public class CategoryServiceTest
{
    private AppDbContext _context = null!;
    private ICategoryRepository _categoryRepository = null!;
    private Mock<IMenuItemRepository> _menuItemRepositoryMock = null!;
    private ICategoryService _categoryService = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _categoryRepository = new CategoryRepository(_context);

        var logger = new Mock<ILogger<CategoryService>>().Object;
        _menuItemRepositoryMock = new Mock<IMenuItemRepository>();
        _menuItemRepositoryMock.Setup(m => m.GetAllAsync()).ReturnsAsync(new List<MenuItem>().AsQueryable());
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        _categoryService = new CategoryService(_categoryRepository, _menuItemRepositoryMock.Object, logger, memoryCache);
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
        var dto = new CategoryCreateDto
        {
            Name = "Appetizers",
            IsNonVeg = false
        };

        var result = await _categoryService.CreateAsync(dto);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Appetizers"));
        Assert.That(result.IsNonVeg, Is.False);
    }

    [Test]
    public void CreateAsync_FailTest_EmptyName_ThrowsArgumentException()
    {
        var dto = new CategoryCreateDto
        {
            Name = "", // Invalid name
            IsNonVeg = false
        };

        Assert.ThrowsAsync<ArgumentException>(async () => await _categoryService.CreateAsync(dto));
    }

    [Test]
    public async Task GetByIdAsync_PassTest()
    {
        var category = new Category
        {
            Id = 3,
            Name = "Beverages",
            IsNonVeg = false
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var result = await _categoryService.GetByIdAsync(3);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(3));
        Assert.That(result.Name, Is.EqualTo("Beverages"));
    }

    [Test]
    public void GetByIdAsync_FailTest_NotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _categoryService.GetByIdAsync(999));
    }

    [Test]
    public async Task UpdateAsync_PassTest()
    {
        var category = new Category
        {
            Id = 1,
            Name = "Soups",
            IsNonVeg = false
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var updateDto = new CategoryUpdateDto
        {
            Name = "Hot Soups",
            IsNonVeg = true
        };

        var result = await _categoryService.UpdateAsync(1, updateDto);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Hot Soups"));
        Assert.That(result.IsNonVeg, Is.True);
    }

    [Test]
    public void UpdateAsync_FailTest_NotFound_ThrowsNotFoundException()
    {
        var updateDto = new CategoryUpdateDto
        {
            Name = "Hot Soups",
            IsNonVeg = true
        };

        Assert.ThrowsAsync<NotFoundException>(async () => await _categoryService.UpdateAsync(999, updateDto));
    }

    [Test]
    public async Task DeleteAsync_PassTest()
    {
        var category = new Category
        {
            Id = 2,
            Name = "Main Course",
            IsNonVeg = true
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var result = await _categoryService.DeleteAsync(2);

        Assert.That(result, Is.True);
        var deletedCategory = await _context.Categories.FindAsync(2);
        Assert.That(deletedCategory!.IsDeleted, Is.True);
    }

    [Test]
    public void DeleteAsync_FailTest_NotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _categoryService.DeleteAsync(999));
    }

    [Test]
    public async Task CreateAsync_DuplicateNameSameType_ThrowsConflictException()
    {
        var category = new Category
        {
            Id = 10,
            Name = "Desserts",
            IsNonVeg = false
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var dto = new CategoryCreateDto
        {
            Name = "Desserts",
            IsNonVeg = false
        };

        Assert.ThrowsAsync<ConflictException>(async () => await _categoryService.CreateAsync(dto));
    }

    [Test]
    public async Task CreateAsync_DuplicateNameDifferentType_Passes()
    {
        var category = new Category
        {
            Id = 11,
            Name = "Desserts",
            IsNonVeg = false
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var dto = new CategoryCreateDto
        {
            Name = "Desserts",
            IsNonVeg = true
        };

        var result = await _categoryService.CreateAsync(dto);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Desserts"));
        Assert.That(result.IsNonVeg, Is.True);
    }

    [Test]
    public async Task CreateAsync_DuplicateNameWithDeletedCategory_Passes()
    {
        var category = new Category
        {
            Id = 12,
            Name = "Beverages",
            IsNonVeg = false,
            IsDeleted = true
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var dto = new CategoryCreateDto
        {
            Name = "Beverages",
            IsNonVeg = false
        };

        var result = await _categoryService.CreateAsync(dto);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Beverages"));
        Assert.That(result.IsNonVeg, Is.False);
    }

    [Test]
    public async Task UpdateAsync_DuplicateNameSameType_ThrowsConflictException()
    {
        var category1 = new Category
        {
            Id = 13,
            Name = "Appetizers",
            IsNonVeg = false
        };
        var category2 = new Category
        {
            Id = 14,
            Name = "Sides",
            IsNonVeg = false
        };
        _context.Categories.AddRange(category1, category2);
        await _context.SaveChangesAsync();

        var updateDto = new CategoryUpdateDto
        {
            Name = "Appetizers",
            IsNonVeg = false
        };

        Assert.ThrowsAsync<ConflictException>(async () => await _categoryService.UpdateAsync(14, updateDto));
    }

    [Test]
    public async Task GetAllAsync_NoFilters_ReturnsAllCategoriesAndCaches()
    {
        // Arrange
        _context.Categories.AddRange(
            new Category { Id = 20, Name = "Veg Category", IsNonVeg = false },
            new Category { Id = 21, Name = "NonVeg Category", IsNonVeg = true }
        );
        await _context.SaveChangesAsync();

        // Act
        var result1 = await _categoryService.GetAllAsync();
        var result2 = await _categoryService.GetAllAsync(); // hits cache

        // Assert
        Assert.That(result1.Count(), Is.EqualTo(2));
        Assert.That(result2.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetAllAsync_FilterVeg_ReturnsVegCategoriesOnly()
    {
        // Arrange
        _context.Categories.AddRange(
            new Category { Id = 22, Name = "Veg Category", IsNonVeg = false },
            new Category { Id = 23, Name = "NonVeg Category", IsNonVeg = true }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _categoryService.GetAllAsync(isNonVeg: false);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().Name, Is.EqualTo("Veg Category"));
    }

    [Test]
    public async Task GetAllAsync_FilterNonVeg_ReturnsNonVegCategoriesOnly()
    {
        // Arrange
        _context.Categories.AddRange(
            new Category { Id = 24, Name = "Veg Category", IsNonVeg = false },
            new Category { Id = 25, Name = "NonVeg Category", IsNonVeg = true }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _categoryService.GetAllAsync(isNonVeg: true);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().Name, Is.EqualTo("NonVeg Category"));
    }

    [Test]
    public void DeleteAsync_CategoryHasActiveMenuItems_ThrowsBusinessRuleException()
    {
        // Arrange
        var category = new Category { Id = 26, Name = "Desserts", IsNonVeg = false };
        _context.Categories.Add(category);
        _context.SaveChanges();

        var mockItem = new MenuItem { Id = 1, Name = "Cake", CategoryId = 26, Price = 10, Description = "Sweet cake", IsDeleted = false, PreparationTime = 10 };
        _menuItemRepositoryMock.Setup(m => m.GetAllAsync()).ReturnsAsync(new List<MenuItem> { mockItem }.AsQueryable());

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(async () => await _categoryService.DeleteAsync(26));
        Assert.That(ex.Message, Is.EqualTo("Category cannot be deleted because it contains active menu items."));
    }

    [Test]
    public async Task CreateAsync_DuplicateNameSameTypeNonVeg_ThrowsConflictException()
    {
        var category = new Category
        {
            Id = 30,
            Name = "Desserts NonVeg",
            IsNonVeg = true
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var dto = new CategoryCreateDto
        {
            Name = "Desserts NonVeg",
            IsNonVeg = true
        };

        var ex = Assert.ThrowsAsync<ConflictException>(async () => await _categoryService.CreateAsync(dto));
        Assert.That(ex.Message, Contains.Substring("Non-Veg"));
    }

    [Test]
    public async Task DeleteAsync_CategoryHasOnlyDeletedMenuItems_DeletesSuccessfully()
    {
        // Arrange
        var category = new Category { Id = 31, Name = "Drinks", IsNonVeg = false };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var deletedItem = new MenuItem { Id = 2, Name = "Soda", CategoryId = 31, Price = 2, Description = "Fizzy", IsDeleted = true, PreparationTime = 5 };
        _menuItemRepositoryMock.Setup(m => m.GetAllAsync()).ReturnsAsync(new List<MenuItem> { deletedItem }.AsQueryable());

        // Act
        var result = await _categoryService.DeleteAsync(31);

        // Assert
        Assert.That(result, Is.True);
        var updated = await _context.Categories.FindAsync(31);
        Assert.That(updated!.IsDeleted, Is.True);
    }

    [Test]
    public async Task UpdateAsync_SameNameAndType_Succeeds()
    {
        // Arrange
        var category = new Category
        {
            Id = 32,
            Name = "Desserts",
            IsNonVeg = false
        };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var updateDto = new CategoryUpdateDto
        {
            Name = "Desserts",
            IsNonVeg = false
        };

        // Act
        var result = await _categoryService.UpdateAsync(32, updateDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Desserts"));
    }
}
