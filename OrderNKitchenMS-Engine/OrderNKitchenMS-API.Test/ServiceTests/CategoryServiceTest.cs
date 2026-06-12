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
public class CategoryServiceTest
{
    private AppDbContext _context = null!;
    private ICategoryRepository _categoryRepository = null!;
    private ICategoryService _categoryService = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "OrderNKitchenDb")
            .Options;

        _context = new AppDbContext(options);
        _categoryRepository = new CategoryRepository(_context);

        var logger = new Mock<ILogger<CategoryService>>().Object;
        _categoryService = new CategoryService(_categoryRepository, logger);
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
}
