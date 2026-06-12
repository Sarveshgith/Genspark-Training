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
public class OrderServiceTest
{
    private AppDbContext _context = null!;
    private IOrderRepository _orderRepository = null!;
    private IOrderItemRepository _orderItemRepository = null!;
    private IItemRepository _itemRepository = null!;
    private IMenuItemIngredientRepository _ingredientRepository = null!;
    private IMenuItemRepository _menuItemRepository = null!;
    private IItemService _itemService = null!;
    private IOrderService _orderService = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "OrderNKitchenDb")
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AppDbContext(options);
        
        _orderItemRepository = new OrderItemRepository(_context);
        _orderRepository = new OrderRepository(_context, _orderItemRepository);
        _itemRepository = new ItemRepository(_context);
        _ingredientRepository = new MenuItemIngredientRepository(_context);
        _menuItemRepository = new MenuItemRepository(_context);

        var itemLogger = new Mock<ILogger<ItemService>>().Object;
        _itemService = new ItemService(_itemRepository, _ingredientRepository, _menuItemRepository, itemLogger, _context);

        var orderLogger = new Mock<ILogger<OrderService>>().Object;
        _orderService = new OrderService(_context, _orderRepository, _orderItemRepository, _itemService, orderLogger);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task CreateOrderAsync_PassTest()
    {
        // Arrange
        _context.Tables.Add(new Table { Id = 1, Number = 1, Capacity = 4, Status = TableStatus.Available });
        _context.Categories.Add(new Category { Id = 1, Name = "Desserts" });
        _context.MenuItems.Add(new MenuItem { Id = 1, Name = "Cake", Price = 10m, IsAvailable = true, CategoryId = 1, PreparationTime = 5 });
        _context.Items.Add(new Item { Id = 1, Name = "Sugar", Unit = ItemUnit.Grams, StockQuantity = 1000m, StockThreshold = 100m, IsActive = true });
        _context.MenuItemIngredients.Add(new MenuItemIngredient { Id = 1, MenuItemId = 1, ItemId = 1, QuantityRequired = 50m });
        await _context.SaveChangesAsync();

        var request = new OrderCreateDto
        {
            OrderItems = new List<OrderItemCreateDto>
            {
                new OrderItemCreateDto { MenuItemId = 1, Quantity = 2 }
            }
        };

        // Act
        var result = await _orderService.CreateOrderAsync(1, request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.TotalAmount, Is.EqualTo(20m));
        var updatedItem = await _context.Items.FindAsync(1);
        Assert.That(updatedItem!.StockQuantity, Is.EqualTo(900m)); // 1000 - 50 * 2 = 900
    }

    [Test]
    public void CreateOrderAsync_FailTest_TableOccupied_ThrowsBusinessRuleException()
    {
        // Arrange
        _context.Tables.Add(new Table { Id = 1, Number = 1, Capacity = 4, Status = TableStatus.Occupied });
        _context.SaveChanges();

        var request = new OrderCreateDto
        {
            OrderItems = new List<OrderItemCreateDto>
            {
                new OrderItemCreateDto { MenuItemId = 1, Quantity = 2 }
            }
        };

        // Act & Assert
        Assert.ThrowsAsync<BusinessRuleException>(async () => await _orderService.CreateOrderAsync(1, request));
    }

    [Test]
    public async Task AddOrderItemsAsync_PassTest()
    {
        // Arrange
        _context.Tables.Add(new Table { Id = 1, Number = 1, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 10, TableId = 1, Status = OrderStatus.Pending, TotalAmount = 10m });
        _context.Categories.Add(new Category { Id = 1, Name = "Drinks" });
        _context.MenuItems.Add(new MenuItem { Id = 2, Name = "Soda", Price = 3m, IsAvailable = true, CategoryId = 1, PreparationTime = 2 });
        _context.Items.Add(new Item { Id = 2, Name = "SodaCan", Unit = ItemUnit.Pieces, StockQuantity = 10m, StockThreshold = 1m, IsActive = true });
        _context.MenuItemIngredients.Add(new MenuItemIngredient { Id = 2, MenuItemId = 2, ItemId = 2, QuantityRequired = 1m });
        await _context.SaveChangesAsync();

        var request = new List<OrderItemCreateDto>
        {
            new OrderItemCreateDto { MenuItemId = 2, Quantity = 3 }
        };

        // Act
        var result = await _orderService.AddOrderItemsAsync(10, request);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.TotalAmount, Is.EqualTo(19m)); // 10 + 3 * 3 = 19
        var updatedItem = await _context.Items.FindAsync(2);
        Assert.That(updatedItem!.StockQuantity, Is.EqualTo(7m)); // 10 - 3 = 7
    }

    [Test]
    public void AddOrderItemsAsync_FailTest_OrderNotFound_ThrowsNotFoundException()
    {
        var request = new List<OrderItemCreateDto>
        {
            new OrderItemCreateDto { MenuItemId = 1, Quantity = 1 }
        };

        Assert.ThrowsAsync<NotFoundException>(async () => await _orderService.AddOrderItemsAsync(999, request));
    }

    [Test]
    public async Task RemoveOrderItemAsync_PassTest()
    {
        // Arrange
        _context.Tables.Add(new Table { Id = 1, Number = 1, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 20, TableId = 1, Status = OrderStatus.Pending, TotalAmount = 15m });
        _context.Categories.Add(new Category { Id = 1, Name = "Desserts" });
        _context.MenuItems.Add(new MenuItem { Id = 1, Name = "Cake", Price = 10m, IsAvailable = true, CategoryId = 1, PreparationTime = 5 });
        _context.Items.Add(new Item { Id = 1, Name = "Sugar", Unit = ItemUnit.Grams, StockQuantity = 500m, StockThreshold = 100m, IsActive = true });
        _context.MenuItemIngredients.Add(new MenuItemIngredient { Id = 1, MenuItemId = 1, ItemId = 1, QuantityRequired = 50m });
        var orderItem = new OrderItem { Id = 5, OrderId = 20, MenuItemId = 1, Quantity = 1, UnitPrice = 10m, Notes = "" };
        _context.OrderItems.Add(orderItem);
        await _context.SaveChangesAsync();

        // Act
        var success = await _orderService.RemoveOrderItemAsync(20, 5);

        // Assert
        Assert.That(success, Is.True);
        var updatedItem = await _context.Items.FindAsync(1);
        Assert.That(updatedItem!.StockQuantity, Is.EqualTo(550m)); // Sugar restored: 500 + 50 = 550
        var order = await _context.Orders.FindAsync(20);
        Assert.That(order!.TotalAmount, Is.EqualTo(5m)); // Total amount updated: 15 - 10 = 5
    }

    [Test]
    public void RemoveOrderItemAsync_FailTest_NotPending_ThrowsBusinessRuleException()
    {
        // Arrange
        _context.Tables.Add(new Table { Id = 1, Number = 1, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 20, TableId = 1, Status = OrderStatus.Completed, TotalAmount = 15m });
        _context.SaveChanges();

        Assert.ThrowsAsync<BusinessRuleException>(async () => await _orderService.RemoveOrderItemAsync(20, 5));
    }
}
