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
        _context.Dispose();
        _connection.Dispose();
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
        var result = await _orderService.CreateOrderAsync(1, 2, request);
 
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.TotalAmount, Is.EqualTo(20m));
        Assert.That(result.EstimatedReadyAt, Is.Not.Null);
        Assert.That(result.EstimatedReadyAt.Value, Is.GreaterThanOrEqualTo(DateTime.UtcNow.AddMinutes(4)));
        var updatedItem = await _context.Items.FindAsync(1);
        await _context.Entry(updatedItem!).ReloadAsync();
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
        Assert.ThrowsAsync<BusinessRuleException>(async () => await _orderService.CreateOrderAsync(1, 2, request));
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
        await _context.Entry(updatedItem!).ReloadAsync();
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
        await _context.Entry(updatedItem!).ReloadAsync();
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

    [Test]
    public async Task AssignChefToOrderAsync_Success()
    {
        // Arrange
        if (await _context.Roles.FindAsync(3) == null)
        {
            _context.Roles.Add(new Role { Id = 3, Name = UserRole.Chef });
        }
        _context.Users.Add(new User { Id = 10, Name = "Chef John", Email = "chef@example.com", PasswordHash = "hash", RoleId = 3 });
        _context.Tables.Add(new Table { Id = 1, Number = 1, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 30, TableId = 1, Status = OrderStatus.Pending, TotalAmount = 10m });
        await _context.SaveChangesAsync();

        // Act
        var result = await _orderService.AssignChefToOrderAsync(30, 10);

        // Assert
        Assert.That(result, Is.True);
        var order = await _context.Orders.FindAsync(30);
        Assert.That(order!.AssignedChefId, Is.EqualTo(10));
        Assert.That(order.Status, Is.EqualTo(OrderStatus.InPrep));
    }

    [Test]
    public async Task AssignWaiterToOrderAsync_Success()
    {
        // Arrange
        if (await _context.Roles.FindAsync(5) == null)
        {
            _context.Roles.Add(new Role { Id = 5, Name = UserRole.Waiter });
        }
        _context.Users.Add(new User { Id = 11, Name = "Waiter Steve", Email = "waiter@example.com", PasswordHash = "hash", RoleId = 5 });
        _context.Tables.Add(new Table { Id = 1, Number = 1, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 31, TableId = 1, Status = OrderStatus.Pending, TotalAmount = 10m });
        await _context.SaveChangesAsync();

        // Act
        var result = await _orderService.AssignWaiterToOrderAsync(31, 11);

        // Assert
        Assert.That(result, Is.True);
        var order = await _context.Orders.FindAsync(31);
        Assert.That(order!.AssignedWaiterId, Is.EqualTo(11));
    }

    [Test]
    public async Task GetGuestOrderTrackingAsync_Success_CalculatesQueueAndTime()
    {
        // Arrange
        _context.Tables.Add(new Table { Id = 2, Number = 2, Capacity = 4, Status = TableStatus.Occupied });
        _context.Tables.Add(new Table { Id = 3, Number = 3, Capacity = 4, Status = TableStatus.Occupied });

        _context.Categories.Add(new Category { Id = 1, Name = "Desserts" });
        _context.MenuItems.Add(new MenuItem { Id = 1, Name = "Cake", Price = 10m, IsAvailable = true, CategoryId = 1, PreparationTime = 15 });
        _context.MenuItems.Add(new MenuItem { Id = 2, Name = "Pie", Price = 8m, IsAvailable = true, CategoryId = 1, PreparationTime = 10 });

        // Add order 1 (Pending)
        var order1 = new Order 
        { 
            Id = 50, 
            TableId = 2, 
            Status = OrderStatus.Pending, 
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        _context.Orders.Add(order1);
        _context.OrderItems.Add(new OrderItem { Id = 50, OrderId = 50, MenuItemId = 1, Quantity = 1, UnitPrice = 10m });

        // Add order 2 (Pending) - Target order
        var order2 = new Order 
        { 
            Id = 51, 
            TableId = 3, 
            Status = OrderStatus.Pending, 
            CreatedAt = DateTime.UtcNow
        };
        _context.Orders.Add(order2);
        _context.OrderItems.Add(new OrderItem { Id = 51, OrderId = 51, MenuItemId = 2, Quantity = 1, UnitPrice = 8m });

        await _context.SaveChangesAsync();

        // Act
        var result = await _orderService.GetGuestOrderTrackingAsync(3);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.OrderId, Is.EqualTo(51));
        Assert.That(result.TableId, Is.EqualTo(3));
        Assert.That(result.QueuePosition, Is.EqualTo(2));
        Assert.That(result.EstimatedTimeMinutes, Is.GreaterThan(0));
        Assert.That(result.OrderItems.Count, Is.EqualTo(1));
        Assert.That(result.OrderItems.First().MenuItemName, Is.EqualTo("Pie"));
    }

    [Test]
    public async Task UpdateOrderStatusAsync_ValidTransitions_Success()
    {
        // Arrange
        _context.Tables.Add(new Table { Id = 1, Number = 1, Capacity = 4, Status = TableStatus.Occupied });
        var order = new Order { Id = 100, TableId = 1, Status = OrderStatus.Pending, TotalAmount = 10m };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act & Assert: Pending -> InPrep
        var res1 = await _orderService.UpdateOrderStatusAsync(100, (int)OrderStatus.InPrep);
        Assert.That(res1, Is.True);
        var dbOrder = await _context.Orders.FindAsync(100);
        Assert.That(dbOrder!.Status, Is.EqualTo(OrderStatus.InPrep));

        // InPrep -> Ready
        var res2 = await _orderService.UpdateOrderStatusAsync(100, (int)OrderStatus.Ready);
        Assert.That(res2, Is.True);
        await _context.Entry(dbOrder).ReloadAsync();
        Assert.That(dbOrder.Status, Is.EqualTo(OrderStatus.Ready));

        // Ready -> Served
        var res3 = await _orderService.UpdateOrderStatusAsync(100, (int)OrderStatus.Served);
        Assert.That(res3, Is.True);
        await _context.Entry(dbOrder).ReloadAsync();
        Assert.That(dbOrder.Status, Is.EqualTo(OrderStatus.Served));

        // Served -> Completed
        var res4 = await _orderService.UpdateOrderStatusAsync(100, (int)OrderStatus.Completed);
        Assert.That(res4, Is.True);
        await _context.Entry(dbOrder).ReloadAsync();
        Assert.That(dbOrder.Status, Is.EqualTo(OrderStatus.Completed));
    }

    [Test]
    public async Task UpdateOrderStatusAsync_InvalidTransition_ThrowsBusinessRuleException()
    {
        // Arrange
        _context.Tables.Add(new Table { Id = 1, Number = 1, Capacity = 4, Status = TableStatus.Occupied });
        var order = new Order { Id = 101, TableId = 1, Status = OrderStatus.Pending, TotalAmount = 10m };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act & Assert: Pending -> Completed (Invalid)
        Assert.ThrowsAsync<BusinessRuleException>(async () => 
            await _orderService.UpdateOrderStatusAsync(101, (int)OrderStatus.Completed));
    }

    [Test]
    public async Task UpdateOrderStatusAsync_CancelOrder_RestoresStock()
    {
        // Arrange
        _context.Tables.Add(new Table { Id = 1, Number = 1, Capacity = 4, Status = TableStatus.Occupied });
        _context.Categories.Add(new Category { Id = 1, Name = "Desserts" });
        _context.MenuItems.Add(new MenuItem { Id = 1, Name = "Cake", Price = 10m, IsAvailable = true, CategoryId = 1, PreparationTime = 5 });
        _context.Items.Add(new Item { Id = 1, Name = "Sugar", Unit = ItemUnit.Grams, StockQuantity = 500m, StockThreshold = 100m, IsActive = true });
        _context.MenuItemIngredients.Add(new MenuItemIngredient { Id = 1, MenuItemId = 1, ItemId = 1, QuantityRequired = 50m });

        var order = new Order { Id = 102, TableId = 1, Status = OrderStatus.Pending, TotalAmount = 10m };
        _context.Orders.Add(order);
        var orderItem = new OrderItem { Id = 1, OrderId = 102, MenuItemId = 1, Quantity = 2, UnitPrice = 10m };
        _context.OrderItems.Add(orderItem);
        await _context.SaveChangesAsync();

        // Act: Cancel order
        var success = await _orderService.UpdateOrderStatusAsync(102, (int)OrderStatus.Cancelled);

        // Assert
        Assert.That(success, Is.True);
        var updatedItem = await _context.Items.FindAsync(1);
        await _context.Entry(updatedItem!).ReloadAsync();
        Assert.That(updatedItem!.StockQuantity, Is.EqualTo(600m)); // 500 + 50 * 2 = 600
    }
}
