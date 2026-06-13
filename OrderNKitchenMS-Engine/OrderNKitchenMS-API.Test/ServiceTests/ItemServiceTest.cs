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
using OrderNKitchenMS_API.Models.Enums;
using OrderNKitchenMS_API.Repositories;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Services;
using OrderNKitchenMS_API.Services.Interfaces;

namespace OrderNKitchenMS_API.Test.ServiceTests;

[TestFixture]
public class ItemServiceTest
{
    private AppDbContext _context = null!;
    private IItemRepository _itemRepository = null!;
    private IMenuItemIngredientRepository _ingredientRepository = null!;
    private IMenuItemRepository _menuItemRepository = null!;
    private IItemService _itemService = null!;

    private Microsoft.Data.Sqlite.SqliteConnection _connection = null!;

    [SetUp]
    public void Setup()
    {
        _connection = new Microsoft.Data.Sqlite.SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
        _context.Database.ExecuteSqlRaw("PRAGMA foreign_keys = OFF;");

        _itemRepository = new ItemRepository(_context);
        _ingredientRepository = new MenuItemIngredientRepository(_context);
        _menuItemRepository = new MenuItemRepository(_context);

        var logger = new Mock<ILogger<ItemService>>().Object;
        _itemService = new ItemService(_itemRepository, _ingredientRepository, _menuItemRepository, logger, _context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Test]
    public async Task CreateItemAsync_PassTest()
    {
        var dto = new ItemCreateDto
        {
            Name = "Onion",
            Unit = ItemUnit.Kilograms,
            StockQuantity = 10m,
            StockThreshold = 1m,
            CostPerUnit = 2.5m
        };

        var result = await _itemService.CreateItemAsync(dto);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Onion"));
        Assert.That(result.StockQuantity, Is.EqualTo(10m));
    }

    [Test]
    public void CreateItemAsync_FailTest_EmptyName_ThrowsArgumentException()
    {
        var dto = new ItemCreateDto
        {
            Name = "", // Invalid
            Unit = ItemUnit.Kilograms,
            StockQuantity = 10m,
            StockThreshold = 1m
        };

        Assert.ThrowsAsync<ArgumentException>(async () => await _itemService.CreateItemAsync(dto));
    }

    [Test]
    public async Task GetItemByIdAsync_PassTest()
    {
        var item = new Item
        {
            Id = 5,
            Name = "Garlic",
            Unit = ItemUnit.Pieces,
            StockQuantity = 50m,
            StockThreshold = 5m,
            IsActive = true
        };
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        var result = await _itemService.GetItemByIdAsync(5);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(5));
        Assert.That(result.Name, Is.EqualTo("Garlic"));
    }

    [Test]
    public void GetItemByIdAsync_FailTest_NotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _itemService.GetItemByIdAsync(999));
    }

    [Test]
    public async Task UpdateStockByMenuItemIdAsync_PassTest()
    {
        var item = new Item
        {
            Id = 1,
            Name = "Rice",
            Unit = ItemUnit.Kilograms,
            StockQuantity = 10m,
            StockThreshold = 1m,
            IsActive = true
        };
        _context.Items.Add(item);

        var ingredient = new MenuItemIngredient
        {
            Id = 1,
            MenuItemId = 1,
            ItemId = 1,
            QuantityRequired = 2m
        };
        _context.MenuItemIngredients.Add(ingredient);
        await _context.SaveChangesAsync();

        // Act: deduct 3 * 2 = 6 kg
        await _itemService.UpdateStockByMenuItemIdAsync(1, 3, false);

        var updatedItem = await _context.Items.FindAsync(1);
        await _context.Entry(updatedItem!).ReloadAsync();
        Assert.That(updatedItem!.StockQuantity, Is.EqualTo(4m));
    }

    [Test]
    public async Task UpdateStockByMenuItemIdAsync_FailTest_InsufficientStock_ThrowsBusinessRuleException()
    {
        var item = new Item
        {
            Id = 1,
            Name = "Rice",
            Unit = ItemUnit.Kilograms,
            StockQuantity = 3m,
            StockThreshold = 1m,
            IsActive = true
        };
        _context.Items.Add(item);

        var ingredient = new MenuItemIngredient
        {
            Id = 1,
            MenuItemId = 1,
            ItemId = 1,
            QuantityRequired = 2m
        };
        _context.MenuItemIngredients.Add(ingredient);
        await _context.SaveChangesAsync();

        // Act: try to deduct 2 * 2 = 4 kg (we only have 3 kg)
        Assert.ThrowsAsync<BusinessRuleException>(async () => await _itemService.UpdateStockByMenuItemIdAsync(1, 2, false));
    }

    [Test]
    public async Task RestockItemAsync_PassTest_IncreasesStock()
    {
        // Arrange
        var item = new Item
        {
            Id = 10,
            Name = "Milk",
            Unit = ItemUnit.Liters,
            StockQuantity = 5m,
            StockThreshold = 1m,
            IsActive = true
        };
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        var restockDto = new ItemRestockDto { Quantity = 15.5m };

        // Act
        var result = await _itemService.RestockItemAsync(10, restockDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StockQuantity, Is.EqualTo(20.5m));
        var updatedItem = await _context.Items.FindAsync(10);
        await _context.Entry(updatedItem!).ReloadAsync();
        Assert.That(updatedItem!.StockQuantity, Is.EqualTo(20.5m));
    }

    [Test]
    public void RestockItemAsync_FailTest_InvalidQuantity_ThrowsArgumentException()
    {
        var restockDto = new ItemRestockDto { Quantity = -5m }; // Invalid
        Assert.ThrowsAsync<ArgumentException>(async () => await _itemService.RestockItemAsync(1, restockDto));
    }

    [Test]
    public void RestockItemAsync_FailTest_ItemNotFound_ThrowsNotFoundException()
    {
        var restockDto = new ItemRestockDto { Quantity = 5m };
        Assert.ThrowsAsync<NotFoundException>(async () => await _itemService.RestockItemAsync(999, restockDto));
    }
}
