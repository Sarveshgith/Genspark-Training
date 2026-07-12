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

        _context.Categories.Add(new Category { Id = 1, Name = "Default Category" });
        _context.SaveChanges();
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

    [Test]
    public async Task CreateItemAsync_DuplicateName_ThrowsConflictException()
    {
        // Arrange
        _context.Items.Add(new Item { Id = 10, Name = "Onion", Unit = ItemUnit.Kilograms, StockQuantity = 10m, StockThreshold = 1m, IsActive = true });
        await _context.SaveChangesAsync();

        var dto = new ItemCreateDto
        {
            Name = "Onion", // Exact duplicate
            Unit = ItemUnit.Kilograms,
            StockQuantity = 5m,
            StockThreshold = 1m
        };

        // Act & Assert
        Assert.ThrowsAsync<ConflictException>(async () => await _itemService.CreateItemAsync(dto));
    }

    [Test]
    public async Task UpdateItemAsync_DuplicateName_ThrowsConflictException()
    {
        // Arrange
        _context.Items.Add(new Item { Id = 10, Name = "Onion", Unit = ItemUnit.Kilograms, StockQuantity = 10m, StockThreshold = 1m, IsActive = true });
        _context.Items.Add(new Item { Id = 11, Name = "Garlic", Unit = ItemUnit.Pieces, StockQuantity = 5m, StockThreshold = 1m, IsActive = true });
        await _context.SaveChangesAsync();

        var dto = new ItemUpdateDto
        {
            Name = "Onion", // Duplicate of ID 10
            Unit = ItemUnit.Pieces,
            StockQuantity = 5m,
            StockThreshold = 1m,
            IsActive = true
        };

        // Act & Assert
        Assert.ThrowsAsync<ConflictException>(async () => await _itemService.UpdateItemAsync(11, dto));
    }

    [Test]
    public async Task GetAllItemsAsync_ReturnsAll()
    {
        _context.Items.AddRange(
            new Item { Id = 30, Name = "Item A", Unit = ItemUnit.Kilograms, StockQuantity = 10, StockThreshold = 1, IsActive = true },
            new Item { Id = 31, Name = "Item B", Unit = ItemUnit.Pieces, StockQuantity = 20, StockThreshold = 2, IsActive = true }
        );
        await _context.SaveChangesAsync();

        var result = await _itemService.GetAllItemsAsync();
        Assert.That(result.Count(), Is.GreaterThanOrEqualTo(2));
    }

    [Test]
    public async Task GetLowStockItemsAsync_ReturnsLowStock()
    {
        _context.Items.AddRange(
            new Item { Id = 32, Name = "Low Item", Unit = ItemUnit.Kilograms, StockQuantity = 1, StockThreshold = 5, IsActive = true },
            new Item { Id = 33, Name = "Normal Item", Unit = ItemUnit.Pieces, StockQuantity = 20, StockThreshold = 2, IsActive = true }
        );
        await _context.SaveChangesAsync();

        var result = await _itemService.GetLowStockItemsAsync();
        Assert.That(result.Any(r => r.Id == 32), Is.True);
        Assert.That(result.Any(r => r.Id == 33), Is.False);
    }

    [Test]
    public async Task UpdateItemAsync_Succeeds()
    {
        _context.Items.Add(new Item { Id = 34, Name = "Old Name", Unit = ItemUnit.Kilograms, StockQuantity = 10, StockThreshold = 1, IsActive = true });
        await _context.SaveChangesAsync();

        var dto = new ItemUpdateDto
        {
            Name = "New Name",
            Unit = ItemUnit.Pieces,
            StockQuantity = 15,
            StockThreshold = 2,
            CostPerUnit = 3.5m,
            IsActive = false
        };

        var result = await _itemService.UpdateItemAsync(34, dto);
        Assert.That(result.Name, Is.EqualTo("New Name"));
        Assert.That(result.Unit, Is.EqualTo(ItemUnit.Pieces));
        Assert.That(result.StockQuantity, Is.EqualTo(15m));
        Assert.That(result.IsActive, Is.False);
    }

    [Test]
    public void UpdateItemAsync_NotFound_ThrowsNotFoundException()
    {
        var dto = new ItemUpdateDto { Name = "Nonexistent", Unit = ItemUnit.Kilograms, StockQuantity = 5, StockThreshold = 1, IsActive = true };
        Assert.ThrowsAsync<NotFoundException>(async () => await _itemService.UpdateItemAsync(999, dto));
    }

    [Test]
    public async Task ChangeItemStatusAsync_Succeeds()
    {
        _context.MenuItems.Add(new MenuItem { Id = 350, Name = "Dish 350", Price = 10m, PreparationTime = 10, CategoryId = 1 });
        _context.Items.Add(new Item { Id = 35, Name = "Status Item", Unit = ItemUnit.Pieces, StockQuantity = 5, StockThreshold = 1, IsActive = true });
        _context.MenuItemIngredients.Add(new MenuItemIngredient { Id = 100, MenuItemId = 350, ItemId = 35, QuantityRequired = 1 });
        await _context.SaveChangesAsync();

        await _itemService.ChangeItemStatusAsync(35, false);

        var updated = await _context.Items.FindAsync(35);
        Assert.That(updated!.IsActive, Is.False);
    }

    [Test]
    public void ChangeItemStatusAsync_NotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _itemService.ChangeItemStatusAsync(999, true));
    }

    [Test]
    public async Task GetIngredientsByMenuItemIdAsync_Succeeds()
    {
        _context.MenuItems.Add(new MenuItem { Id = 50, Name = "Dish 50", Price = 10m, PreparationTime = 10, CategoryId = 1 });
        _context.Items.Add(new Item { Id = 36, Name = "Ingredient Item", Unit = ItemUnit.Pieces, StockQuantity = 5, StockThreshold = 1, IsActive = true });
        _context.MenuItemIngredients.Add(new MenuItemIngredient { Id = 10, MenuItemId = 50, ItemId = 36, QuantityRequired = 2 });
        await _context.SaveChangesAsync();

        var result = await _itemService.GetIngredientsByMenuItemIdAsync(50);
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().QuantityRequired, Is.EqualTo(2m));
    }

    [Test]
    public async Task AddMenuItemIngredientsAsync_Succeeds()
    {
        _context.MenuItems.Add(new MenuItem { Id = 40, Name = "Dish 40", Price = 10m, PreparationTime = 10, CategoryId = 1 });
        _context.Items.Add(new Item { Id = 41, Name = "Ingredient 41", Unit = ItemUnit.Pieces, StockQuantity = 10, StockThreshold = 1, IsActive = true });
        await _context.SaveChangesAsync();

        var list = new List<MenuItemIngredientCreateDto>
        {
            new MenuItemIngredientCreateDto { ItemId = 41, QuantityRequired = 3m }
        };

        var result = await _itemService.AddMenuItemIngredientsAsync(40, list);
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().QuantityRequired, Is.EqualTo(3m));
    }

    [Test]
    public void AddMenuItemIngredientsAsync_MenuItemNotFound_ThrowsNotFoundException()
    {
        var list = new List<MenuItemIngredientCreateDto> { new MenuItemIngredientCreateDto { ItemId = 1, QuantityRequired = 1 } };
        Assert.ThrowsAsync<NotFoundException>(async () => await _itemService.AddMenuItemIngredientsAsync(999, list));
    }

    [Test]
    public async Task AddMenuItemIngredientsAsync_ItemNotFound_ThrowsNotFoundException()
    {
        _context.MenuItems.Add(new MenuItem { Id = 42, Name = "Dish 42", Price = 10m, PreparationTime = 10, CategoryId = 1 });
        await _context.SaveChangesAsync();

        var list = new List<MenuItemIngredientCreateDto> { new MenuItemIngredientCreateDto { ItemId = 999, QuantityRequired = 1 } };
        Assert.ThrowsAsync<NotFoundException>(async () => await _itemService.AddMenuItemIngredientsAsync(42, list));
    }

    [Test]
    public async Task AddMenuItemIngredientsAsync_DuplicateIngredient_ThrowsConflictException()
    {
        _context.MenuItems.Add(new MenuItem { Id = 43, Name = "Dish 43", Price = 10m, PreparationTime = 10, CategoryId = 1 });
        _context.Items.Add(new Item { Id = 44, Name = "Ingredient 44", Unit = ItemUnit.Pieces, StockQuantity = 10, StockThreshold = 1, IsActive = true });
        _context.MenuItemIngredients.Add(new MenuItemIngredient { Id = 15, MenuItemId = 43, ItemId = 44, QuantityRequired = 2 });
        await _context.SaveChangesAsync();

        var list = new List<MenuItemIngredientCreateDto> { new MenuItemIngredientCreateDto { ItemId = 44, QuantityRequired = 1 } };
        Assert.ThrowsAsync<BusinessRuleException>(async () => await _itemService.AddMenuItemIngredientsAsync(43, list));
    }

    [Test]
    public async Task UpdateMenuItemIngredientsAsync_Succeeds()
    {
        _context.MenuItems.Add(new MenuItem { Id = 45, Name = "Dish 45", Price = 10m, PreparationTime = 10, CategoryId = 1 });
        _context.Items.Add(new Item { Id = 46, Name = "Ingredient 46", Unit = ItemUnit.Pieces, StockQuantity = 10, StockThreshold = 1, IsActive = true });
        _context.MenuItemIngredients.Add(new MenuItemIngredient { Id = 16, MenuItemId = 45, ItemId = 46, QuantityRequired = 2 });
        await _context.SaveChangesAsync();

        var list = new List<MenuItemIngredientCreateDto>
        {
            new MenuItemIngredientCreateDto { ItemId = 46, QuantityRequired = 5m }
        };

        var result = await _itemService.UpdateMenuItemIngredientsAsync(45, list);
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().QuantityRequired, Is.EqualTo(5m));
    }

    [Test]
    public void UpdateMenuItemIngredientsAsync_MenuItemNotFound_ThrowsNotFoundException()
    {
        var list = new List<MenuItemIngredientCreateDto> { new MenuItemIngredientCreateDto { ItemId = 1, QuantityRequired = 1 } };
        Assert.ThrowsAsync<NotFoundException>(async () => await _itemService.UpdateMenuItemIngredientsAsync(999, list));
    }

    [Test]
    public async Task UpdateMenuItemIngredientsAsync_ItemNotFound_ThrowsNotFoundException()
    {
        _context.MenuItems.Add(new MenuItem { Id = 47, Name = "Dish 47", Price = 10m, PreparationTime = 10, CategoryId = 1 });
        await _context.SaveChangesAsync();

        var list = new List<MenuItemIngredientCreateDto> { new MenuItemIngredientCreateDto { ItemId = 999, QuantityRequired = 1 } };
        Assert.ThrowsAsync<NotFoundException>(async () => await _itemService.UpdateMenuItemIngredientsAsync(47, list));
    }

    [Test]
    public async Task RemoveMenuItemIngredientAsync_Succeeds()
    {
        _context.MenuItems.Add(new MenuItem { Id = 48, Name = "Dish 48", Price = 10m, PreparationTime = 10, CategoryId = 1 });
        _context.Items.Add(new Item { Id = 49, Name = "Ingredient 49", Unit = ItemUnit.Pieces, StockQuantity = 10, StockThreshold = 1, IsActive = true });
        _context.MenuItemIngredients.Add(new MenuItemIngredient { Id = 17, MenuItemId = 48, ItemId = 49, QuantityRequired = 2 });
        await _context.SaveChangesAsync();

        await _itemService.RemoveMenuItemIngredientAsync(48, 17);

        var mapping = await _context.MenuItemIngredients.FindAsync(17);
        Assert.That(mapping, Is.Null);
    }

    [Test]
    public void RemoveMenuItemIngredientAsync_MenuItemNotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _itemService.RemoveMenuItemIngredientAsync(999, 1));
    }

    [Test]
    public async Task RemoveMenuItemIngredientAsync_IngredientNotFound_ThrowsNotFoundException()
    {
        _context.MenuItems.Add(new MenuItem { Id = 50, Name = "Dish 50", Price = 10m, PreparationTime = 10, CategoryId = 1 });
        await _context.SaveChangesAsync();

        Assert.ThrowsAsync<NotFoundException>(async () => await _itemService.RemoveMenuItemIngredientAsync(50, 999));
    }

    [Test]
    public async Task CanPrepareMenuItemAsync_NoIngredients_ReturnsFalse()
    {
        var result = await _itemService.CanPrepareMenuItemAsync(999, 1);
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CanPrepareMenuItemAsync_InactiveIngredientItem_ReturnsFalse()
    {
        _context.Items.Add(new Item { Id = 60, Name = "Ingredient 60", Unit = ItemUnit.Pieces, StockQuantity = 10, StockThreshold = 1, IsActive = false }); // INACTIVE
        _context.MenuItemIngredients.Add(new MenuItemIngredient { Id = 18, MenuItemId = 51, ItemId = 60, QuantityRequired = 1 });
        await _context.SaveChangesAsync();

        var result = await _itemService.CanPrepareMenuItemAsync(51, 1);
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ReevaluateMenuItemAvailabilityAsync_NoopPaths()
    {
        // Arrange
        _context.MenuItems.Add(new MenuItem { Id = 70, Name = "Dish 70", Price = 10m, PreparationTime = 10, IsAvailable = true, IsManuallyDisabled = true, CategoryId = 1 });
        _context.Items.Add(new Item { Id = 71, Name = "Ingredient 71", Unit = ItemUnit.Pieces, StockQuantity = 10, StockThreshold = 1, IsActive = true });
        _context.MenuItemIngredients.Add(new MenuItemIngredient { Id = 19, MenuItemId = 70, ItemId = 71, QuantityRequired = 1 });
        await _context.SaveChangesAsync();

        // Act (When manual override is enabled)
        await _itemService.ReevaluateMenuItemAvailabilityAsync(70);
        var updated = await _context.MenuItems.FindAsync(70);
        Assert.That(updated!.IsAvailable, Is.False); // Still false because of manual override

        // Act (When menu item not found)
        await _itemService.ReevaluateMenuItemAvailabilityAsync(999); // Noop
    }

    [Test]
    public async Task ValidateStockForOrderItemsAsync_NullOrEmpty_Noop()
    {
        await _itemService.ValidateStockForOrderItemsAsync(null!);
        await _itemService.ValidateStockForOrderItemsAsync(new List<OrderItemCreateDto>());
    }

    [Test]
    public void ValidateStockForOrderItemsAsync_MenuItemNotFound_ThrowsNotFoundException()
    {
        var list = new List<OrderItemCreateDto> { new OrderItemCreateDto { MenuItemId = 999, Quantity = 1 } };
        Assert.ThrowsAsync<NotFoundException>(async () => await _itemService.ValidateStockForOrderItemsAsync(list));
    }

    [Test]
    public async Task ValidateStockForOrderItemsAsync_MenuItemNotAvailable_ThrowsBusinessRuleException()
    {
        _context.MenuItems.Add(new MenuItem { Id = 80, Name = "Dish 80", Price = 10m, PreparationTime = 10, IsAvailable = false, CategoryId = 1 });
        await _context.SaveChangesAsync();

        var list = new List<OrderItemCreateDto> { new OrderItemCreateDto { MenuItemId = 80, Quantity = 1 } };
        Assert.ThrowsAsync<BusinessRuleException>(async () => await _itemService.ValidateStockForOrderItemsAsync(list));
    }

    [Test]
    public async Task ValidateStockForOrderItemsAsync_NoIngredients_ThrowsBusinessRuleException()
    {
        _context.MenuItems.Add(new MenuItem { Id = 81, Name = "Dish 81", Price = 10m, PreparationTime = 10, IsAvailable = true, CategoryId = 1 });
        await _context.SaveChangesAsync();

        var list = new List<OrderItemCreateDto> { new OrderItemCreateDto { MenuItemId = 81, Quantity = 1 } };
        Assert.ThrowsAsync<BusinessRuleException>(async () => await _itemService.ValidateStockForOrderItemsAsync(list));
    }

    [Test]
    public async Task ValidateStockForOrderItemsAsync_InactiveIngredient_ThrowsBusinessRuleException()
    {
        _context.MenuItems.Add(new MenuItem { Id = 82, Name = "Dish 82", Price = 10m, PreparationTime = 10, IsAvailable = true, CategoryId = 1 });
        _context.Items.Add(new Item { Id = 83, Name = "Ingredient 83", Unit = ItemUnit.Pieces, StockQuantity = 10, StockThreshold = 1, IsActive = false }); // Inactive
        _context.MenuItemIngredients.Add(new MenuItemIngredient { Id = 20, MenuItemId = 82, ItemId = 83, QuantityRequired = 1 });
        await _context.SaveChangesAsync();

        var list = new List<OrderItemCreateDto> { new OrderItemCreateDto { MenuItemId = 82, Quantity = 1 } };
        Assert.ThrowsAsync<BusinessRuleException>(async () => await _itemService.ValidateStockForOrderItemsAsync(list));
    }

    [Test]
    public async Task ValidateStockForOrderItemsAsync_InsufficientStock_ThrowsBusinessRuleException()
    {
        _context.MenuItems.Add(new MenuItem { Id = 84, Name = "Dish 84", Price = 10m, PreparationTime = 10, IsAvailable = true, CategoryId = 1 });
        _context.Items.Add(new Item { Id = 85, Name = "Ingredient 85", Unit = ItemUnit.Pieces, StockQuantity = 2, StockThreshold = 1, IsActive = true });
        _context.MenuItemIngredients.Add(new MenuItemIngredient { Id = 21, MenuItemId = 84, ItemId = 85, QuantityRequired = 3 }); // requires 3 but stock is 2
        await _context.SaveChangesAsync();

        var list = new List<OrderItemCreateDto> { new OrderItemCreateDto { MenuItemId = 84, Quantity = 1 } };
        Assert.ThrowsAsync<BusinessRuleException>(async () => await _itemService.ValidateStockForOrderItemsAsync(list));
    }

    [Test]
    public async Task ChangeItemStatusAsync_Succeeds_True()
    {
        _context.Items.Add(new Item { Id = 100, Name = "Inactive Item", Unit = ItemUnit.Pieces, StockQuantity = 5, StockThreshold = 1, IsActive = false });
        await _context.SaveChangesAsync();

        await _itemService.ChangeItemStatusAsync(100, true);

        var updated = await _context.Items.FindAsync(100);
        Assert.That(updated!.IsActive, Is.True);
    }

    [Test]
    public void GetIngredientsByMenuItemIdAsync_MenuItemNotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _itemService.GetIngredientsByMenuItemIdAsync(999));
    }

    [Test]
    public async Task UpdateMenuItemIngredientsAsync_DuplicateIngredient_ThrowsBusinessRuleException()
    {
        _context.MenuItems.Add(new MenuItem { Id = 101, Name = "Dish 101", Price = 10m, PreparationTime = 10, CategoryId = 1 });
        _context.Items.Add(new Item { Id = 102, Name = "Ingredient 102", Unit = ItemUnit.Pieces, StockQuantity = 10, StockThreshold = 1, IsActive = true });
        await _context.SaveChangesAsync();

        var list = new List<MenuItemIngredientCreateDto>
        {
            new MenuItemIngredientCreateDto { ItemId = 102, QuantityRequired = 1m },
            new MenuItemIngredientCreateDto { ItemId = 102, QuantityRequired = 2m }
        };

        Assert.ThrowsAsync<BusinessRuleException>(async () => await _itemService.UpdateMenuItemIngredientsAsync(101, list));
    }

    [Test]
    public async Task UpdateMenuItemIngredientsAsync_TransactionRollback_Throws()
    {
        var mockIngredientRepo = new Mock<IMenuItemIngredientRepository>();
        mockIngredientRepo.Setup(r => r.GetByMenuItemIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("Database connection failure"));

        var serviceWithMock = new ItemService(_itemRepository, mockIngredientRepo.Object, _menuItemRepository, new Mock<ILogger<ItemService>>().Object, _context);

        _context.MenuItems.Add(new MenuItem { Id = 103, Name = "Dish 103", Price = 10m, PreparationTime = 10, CategoryId = 1 });
        _context.Items.Add(new Item { Id = 104, Name = "Ingredient 104", Unit = ItemUnit.Pieces, StockQuantity = 10, StockThreshold = 1, IsActive = true });
        await _context.SaveChangesAsync();

        var list = new List<MenuItemIngredientCreateDto>
        {
            new MenuItemIngredientCreateDto { ItemId = 104, QuantityRequired = 1m }
        };

        Assert.ThrowsAsync<Exception>(async () => await serviceWithMock.UpdateMenuItemIngredientsAsync(103, list));
    }

    [Test]
    public async Task CanPrepareMenuItemAsync_InsufficientStock_ReturnsFalse()
    {
        _context.MenuItems.Add(new MenuItem { Id = 105, Name = "Dish 105", Price = 10m, PreparationTime = 10, CategoryId = 1 });
        _context.Items.Add(new Item { Id = 106, Name = "Ingredient 106", Unit = ItemUnit.Pieces, StockQuantity = 1, StockThreshold = 1, IsActive = true });
        _context.MenuItemIngredients.Add(new MenuItemIngredient { Id = 30, MenuItemId = 105, ItemId = 106, QuantityRequired = 5 });
        await _context.SaveChangesAsync();

        var result = await _itemService.CanPrepareMenuItemAsync(105, 1);
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CanPrepareMenuItemAsync_Succeeds_ReturnsTrue()
    {
        _context.MenuItems.Add(new MenuItem { Id = 107, Name = "Dish 107", Price = 10m, PreparationTime = 10, CategoryId = 1 });
        _context.Items.Add(new Item { Id = 108, Name = "Ingredient 108", Unit = ItemUnit.Pieces, StockQuantity = 10, StockThreshold = 1, IsActive = true });
        _context.MenuItemIngredients.Add(new MenuItemIngredient { Id = 31, MenuItemId = 107, ItemId = 108, QuantityRequired = 2 });
        await _context.SaveChangesAsync();

        var result = await _itemService.CanPrepareMenuItemAsync(107, 1);
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task UpdateStockByMenuItemIdAsync_NoIngredients_Returns()
    {
        await _itemService.UpdateStockByMenuItemIdAsync(999, 1, true);
    }
}
