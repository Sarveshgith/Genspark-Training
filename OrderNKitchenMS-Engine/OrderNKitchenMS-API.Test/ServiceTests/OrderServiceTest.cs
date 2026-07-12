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

        var signalLogger = new Mock<ISignalService>().Object;
        var orderLogger = new Mock<ILogger<OrderService>>().Object;
        _orderService = new OrderService(_context, _orderRepository, _orderItemRepository, _itemService, signalLogger, orderLogger);
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

    [Test]
    public void CreateOrderAsync_DuplicateItemsInPayload_ThrowsArgumentException()
    {
        // Arrange
        var request = new OrderCreateDto
        {
            OrderItems = new List<OrderItemCreateDto>
            {
                new OrderItemCreateDto { MenuItemId = 1, Quantity = 2 },
                new OrderItemCreateDto { MenuItemId = 1, Quantity = 1 } // Duplicate MenuItemId
            }
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => await _orderService.CreateOrderAsync(1, 2, request));
    }

    [Test]
    public async Task AddOrderItemsAsync_DuplicateItemsInPayload_ThrowsArgumentException()
    {
        // Arrange
        _context.Tables.Add(new Table { Id = 1, Number = 1, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 10, TableId = 1, Status = OrderStatus.Pending, TotalAmount = 10m });
        await _context.SaveChangesAsync();

        var request = new List<OrderItemCreateDto>
        {
            new OrderItemCreateDto { MenuItemId = 1, Quantity = 1 },
            new OrderItemCreateDto { MenuItemId = 1, Quantity = 2 } // Duplicate
        };

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => await _orderService.AddOrderItemsAsync(10, request));
    }

    [Test]
    public async Task AddOrderItemsAsync_ExistingItemInOrder_ConsolidatesQuantityAndNotes()
    {
        // Arrange
        _context.Tables.Add(new Table { Id = 1, Number = 1, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 10, TableId = 1, Status = OrderStatus.Pending, TotalAmount = 10m });
        _context.Categories.Add(new Category { Id = 1, Name = "Drinks" });
        _context.MenuItems.Add(new MenuItem { Id = 2, Name = "Soda", Price = 3m, IsAvailable = true, CategoryId = 1, PreparationTime = 2 });
        _context.Items.Add(new Item { Id = 2, Name = "SodaCan", Unit = ItemUnit.Pieces, StockQuantity = 10m, StockThreshold = 1m, IsActive = true });
        _context.MenuItemIngredients.Add(new MenuItemIngredient { Id = 2, MenuItemId = 2, ItemId = 2, QuantityRequired = 1m });
        
        // Add existing order item
        _context.OrderItems.Add(new OrderItem { Id = 15, OrderId = 10, MenuItemId = 2, Quantity = 1, UnitPrice = 3m, Notes = "No Ice" });
        await _context.SaveChangesAsync();

        var request = new List<OrderItemCreateDto>
        {
            new OrderItemCreateDto { MenuItemId = 2, Quantity = 2, Notes = "Extra Cold" }
        };

        // Act: add items
        var result = await _orderService.AddOrderItemsAsync(10, request);

        // Assert
        Assert.That(result, Is.Not.Null);
        var orderItems = await _context.OrderItems.Where(oi => oi.OrderId == 10).ToListAsync();
        Assert.That(orderItems.Count, Is.EqualTo(1)); // Consolidated to 1 row
        Assert.That(orderItems[0].Quantity, Is.EqualTo(3)); // 1 + 2 = 3
        Assert.That(orderItems[0].Notes, Is.EqualTo("No Ice; Extra Cold"));
    }

    [Test]
    public void CreateOrderAsync_TableNotFound_ThrowsNotFoundException()
    {
        var request = new OrderCreateDto
        {
            OrderItems = new List<OrderItemCreateDto> { new OrderItemCreateDto { MenuItemId = 1, Quantity = 2 } }
        };
        Assert.ThrowsAsync<NotFoundException>(async () => await _orderService.CreateOrderAsync(999, 2, request));
    }

    [Test]
    public async Task CreateOrderAsync_TransactionRollback_Throws()
    {
        // We can simulate an error by making OrderRepository throw on AddAsync
        var mockOrderRepo = new Mock<IOrderRepository>();
        mockOrderRepo.Setup(r => r.AddAsync(It.IsAny<Order>())).ThrowsAsync(new Exception("DB Error"));

        var service = new OrderService(_context, mockOrderRepo.Object, _orderItemRepository, _itemService, new Mock<ISignalService>().Object, new Mock<ILogger<OrderService>>().Object);

        _context.Tables.Add(new Table { Id = 100, Number = 100, Capacity = 4, Status = TableStatus.Available });
        _context.Categories.Add(new Category { Id = 1, Name = "Desserts" });
        _context.MenuItems.Add(new MenuItem { Id = 1, Name = "Cake", Price = 10m, IsAvailable = true, CategoryId = 1, PreparationTime = 5 });
        _context.Items.Add(new Item { Id = 1, Name = "Sugar", Unit = ItemUnit.Grams, StockQuantity = 1000m, StockThreshold = 100m, IsActive = true });
        _context.MenuItemIngredients.Add(new MenuItemIngredient { Id = 1, MenuItemId = 1, ItemId = 1, QuantityRequired = 50m });
        await _context.SaveChangesAsync();

        var request = new OrderCreateDto
        {
            OrderItems = new List<OrderItemCreateDto> { new OrderItemCreateDto { MenuItemId = 1, Quantity = 2 } }
        };

        Assert.ThrowsAsync<Exception>(async () => await service.CreateOrderAsync(100, 2, request));
    }

    [Test]
    public async Task GetOrderByIdAsync_Success()
    {
        _context.Tables.Add(new Table { Id = 101, Number = 101, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 101, TableId = 101, Status = OrderStatus.Pending, TotalAmount = 10m });
        _context.Categories.Add(new Category { Id = 2, Name = "Food" });
        _context.MenuItems.Add(new MenuItem { Id = 101, Name = "Pizza", Price = 10m, CategoryId = 2, PreparationTime = 10 });
        _context.OrderItems.Add(new OrderItem { Id = 101, OrderId = 101, MenuItemId = 101, Quantity = 1, UnitPrice = 10m });
        await _context.SaveChangesAsync();

        var result = await _orderService.GetOrderByIdAsync(101);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(101));
        Assert.That(result.OrderItems.First().MenuItemName, Is.EqualTo("Pizza"));
    }

    [Test]
    public void GetOrderByIdAsync_NotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _orderService.GetOrderByIdAsync(999));
    }

    [Test]
    public void GetAllOrdersAsync_WithNullQuery_ThrowsNullReferenceException()
    {
        Assert.ThrowsAsync<NullReferenceException>(async () => await _orderService.GetAllOrdersAsync(null!));
    }

    [Test]
    public async Task GetAllOrdersAsync_WithVariousFilters_ReturnsFilteredOrders()
    {
        // Arrange
        _context.Tables.Add(new Table { Id = 102, Number = 102, Capacity = 4, Status = TableStatus.Occupied });
        _context.Tables.Add(new Table { Id = 103, Number = 103, Capacity = 2, Status = TableStatus.Occupied });

        var order1 = new Order { Id = 102, TableId = 102, Status = OrderStatus.Pending, TotalAmount = 10m, CreatedAt = DateTime.UtcNow.AddHours(-2) };
        var order2 = new Order { Id = 103, TableId = 103, Status = OrderStatus.InPrep, TotalAmount = 20m, CreatedAt = DateTime.UtcNow.AddHours(-1) };
        _context.Orders.AddRange(order1, order2);

        _context.Categories.Add(new Category { Id = 3, Name = "Snacks" });
        _context.MenuItems.Add(new MenuItem { Id = 102, Name = "Fries", Price = 5m, CategoryId = 3, PreparationTime = 5 });
        _context.OrderItems.Add(new OrderItem { Id = 102, OrderId = 102, MenuItemId = 102, Quantity = 2, UnitPrice = 5m });
        _context.OrderItems.Add(new OrderItem { Id = 103, OrderId = 103, MenuItemId = 102, Quantity = 4, UnitPrice = 5m });
        await _context.SaveChangesAsync();

        // Query 1: Filter by Status
        var queryStatus = new QueryOrderDto { Status = (int)OrderStatus.InPrep };
        var resStatus = await _orderService.GetAllOrdersAsync(queryStatus);
        Assert.That(resStatus.Count(), Is.EqualTo(1));
        Assert.That(resStatus.First().Id, Is.EqualTo(103));

        // Query 2: Filter by TableId
        var queryTable = new QueryOrderDto { TableId = 102 };
        var resTable = await _orderService.GetAllOrdersAsync(queryTable);
        Assert.That(resTable.Count(), Is.EqualTo(1));
        Assert.That(resTable.First().Id, Is.EqualTo(102));

        // Query 3: Filter by From date
        var queryFrom = new QueryOrderDto { From = DateTime.UtcNow.AddMinutes(-90) };
        var resFrom = await _orderService.GetAllOrdersAsync(queryFrom);
        Assert.That(resFrom.Count(), Is.EqualTo(1));
        Assert.That(resFrom.First().Id, Is.EqualTo(103));

        // Query 4: Filter by To date
        var queryTo = new QueryOrderDto { To = DateTime.UtcNow.AddMinutes(-90) };
        var resTo = await _orderService.GetAllOrdersAsync(queryTo);
        Assert.That(resTo.Count(), Is.EqualTo(1));
        Assert.That(resTo.First().Id, Is.EqualTo(102));

        // Query 5: Paging defaults (PageNumber=0, PageSize=0)
        var queryPaging = new QueryOrderDto { PageNumber = 0, PageSize = 0 };
        var resPaging = await _orderService.GetAllOrdersAsync(queryPaging);
        Assert.That(resPaging.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetActiveOrdersAsync_ReturnsOnlyActive()
    {
        _context.Tables.Add(new Table { Id = 104, Number = 104, Capacity = 4, Status = TableStatus.Occupied });
        var active1 = new Order { Id = 104, TableId = 104, Status = OrderStatus.InPrep, TotalAmount = 10m, CreatedAt = DateTime.UtcNow.AddMinutes(-5) };
        var active2 = new Order { Id = 120, TableId = 104, Status = OrderStatus.Pending, TotalAmount = 5m, CreatedAt = DateTime.UtcNow };
        var completed = new Order { Id = 105, TableId = 104, Status = OrderStatus.Completed, TotalAmount = 10m };
        var cancelled = new Order { Id = 106, TableId = 104, Status = OrderStatus.Cancelled, TotalAmount = 10m };
        _context.Orders.AddRange(active1, active2, completed, cancelled);

        _context.Categories.Add(new Category { Id = 4, Name = "Drinks" });
        _context.MenuItems.Add(new MenuItem { Id = 104, Name = "Water", Price = 2m, CategoryId = 4, PreparationTime = 1 });
        _context.OrderItems.Add(new OrderItem { Id = 104, OrderId = 104, MenuItemId = 104, Quantity = 5, UnitPrice = 2m });
        _context.OrderItems.Add(new OrderItem { Id = 120, OrderId = 120, MenuItemId = 104, Quantity = 2, UnitPrice = 2m });
        await _context.SaveChangesAsync();

        var result = await _orderService.GetActiveOrdersAsync();
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.First().Id, Is.EqualTo(120)); // Sorted descending by CreatedAt
    }

    [Test]
    public void RemoveOrderItemAsync_OrderNotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _orderService.RemoveOrderItemAsync(999, 1));
    }

    [Test]
    public async Task RemoveOrderItemAsync_OrderItemNotFound_ThrowsNotFoundException()
    {
        _context.Tables.Add(new Table { Id = 105, Number = 105, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 107, TableId = 105, Status = OrderStatus.Pending });
        await _context.SaveChangesAsync();

        Assert.ThrowsAsync<NotFoundException>(async () => await _orderService.RemoveOrderItemAsync(107, 999));
    }

    [Test]
    public async Task RemoveOrderItemAsync_OrderItemFromDifferentOrder_ThrowsNotFoundException()
    {
        _context.Tables.Add(new Table { Id = 106, Number = 106, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 108, TableId = 106, Status = OrderStatus.Pending });
        _context.Orders.Add(new Order { Id = 109, TableId = 106, Status = OrderStatus.Pending });
        _context.Categories.Add(new Category { Id = 5, Name = "IceCream" });
        _context.MenuItems.Add(new MenuItem { Id = 105, Name = "Cone", Price = 3m, CategoryId = 5, PreparationTime = 2 });
        var oi = new OrderItem { Id = 105, OrderId = 109, MenuItemId = 105, Quantity = 1, UnitPrice = 3m }; // belongs to order 109
        _context.OrderItems.Add(oi);
        await _context.SaveChangesAsync();

        // Try to remove from order 108 (different order)
        Assert.ThrowsAsync<NotFoundException>(async () => await _orderService.RemoveOrderItemAsync(108, 105));
    }

    [Test]
    public async Task RemoveOrderItemAsync_TransactionRollback_Throws()
    {
        var mockItemService = new Mock<IItemService>();
        mockItemService.Setup(s => s.UpdateStockByMenuItemIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("Stock rollback failed"));

        var service = new OrderService(_context, _orderRepository, _orderItemRepository, mockItemService.Object, new Mock<ISignalService>().Object, new Mock<ILogger<OrderService>>().Object);

        _context.Tables.Add(new Table { Id = 107, Number = 107, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 110, TableId = 107, Status = OrderStatus.Pending });
        _context.Categories.Add(new Category { Id = 6, Name = "Desserts" });
        _context.MenuItems.Add(new MenuItem { Id = 106, Name = "Pudding", Price = 4m, CategoryId = 6, PreparationTime = 3 });
        _context.OrderItems.Add(new OrderItem { Id = 106, OrderId = 110, MenuItemId = 106, Quantity = 1, UnitPrice = 4m });
        await _context.SaveChangesAsync();

        Assert.ThrowsAsync<Exception>(async () => await service.RemoveOrderItemAsync(110, 106));
    }

    [Test]
    public async Task RemoveOrderItemAsync_DeleteFailure_ThrowsNotFoundException()
    {
        var mockOrderItemRepo = new Mock<IOrderItemRepository>();
        mockOrderItemRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new OrderItem { Id = 107, OrderId = 111, MenuItemId = 106, Quantity = 1, UnitPrice = 4m });
        mockOrderItemRepo.Setup(r => r.DeleteAsync(It.IsAny<int>()))
            .ReturnsAsync(false); // Simulate delete returning false

        var service = new OrderService(_context, _orderRepository, mockOrderItemRepo.Object, _itemService, new Mock<ISignalService>().Object, new Mock<ILogger<OrderService>>().Object);

        _context.Tables.Add(new Table { Id = 108, Number = 108, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 111, TableId = 108, Status = OrderStatus.Pending });
        await _context.SaveChangesAsync();

        Assert.ThrowsAsync<NotFoundException>(async () => await service.RemoveOrderItemAsync(111, 107));
    }

    [Test]
    public void AddOrderItemsAsync_NullPayload_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(async () => await _orderService.AddOrderItemsAsync(1, null!));
    }

    [Test]
    public void AddOrderItemsAsync_EmptyPayload_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(async () => await _orderService.AddOrderItemsAsync(1, new List<OrderItemCreateDto>()));
    }

    [Test]
    public void AddOrderItemsAsync_NullItemInPayload_ThrowsArgumentException()
    {
        var list = new List<OrderItemCreateDto> { null! };
        Assert.ThrowsAsync<ArgumentException>(async () => await _orderService.AddOrderItemsAsync(1, list));
    }

    [Test]
    public async Task AddOrderItemsAsync_OrderNotPending_ThrowsBusinessRuleException()
    {
        _context.Tables.Add(new Table { Id = 109, Number = 109, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 112, TableId = 109, Status = OrderStatus.InPrep });
        await _context.SaveChangesAsync();

        var list = new List<OrderItemCreateDto> { new OrderItemCreateDto { MenuItemId = 1, Quantity = 1 } };
        Assert.ThrowsAsync<BusinessRuleException>(async () => await _orderService.AddOrderItemsAsync(112, list));
    }

    [Test]
    public async Task AddOrderItemsAsync_ExistingItemInOrder_NotesConsolidation()
    {
        _context.Tables.Add(new Table { Id = 110, Number = 110, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 113, TableId = 110, Status = OrderStatus.Pending, TotalAmount = 5m });
        _context.Categories.Add(new Category { Id = 7, Name = "Mains" });
        _context.MenuItems.Add(new MenuItem { Id = 107, Name = "Pasta", Price = 8m, CategoryId = 7, PreparationTime = 12, IsAvailable = true });
        _context.Items.Add(new Item { Id = 107, Name = "Noodles", Unit = ItemUnit.Grams, StockQuantity = 1000m, StockThreshold = 100m, IsActive = true });
        _context.MenuItemIngredients.Add(new MenuItemIngredient { Id = 107, MenuItemId = 107, ItemId = 107, QuantityRequired = 100m });
        
        // Scenario A: Existing order item has empty notes, adding with notes
        _context.OrderItems.Add(new OrderItem { Id = 108, OrderId = 113, MenuItemId = 107, Quantity = 1, UnitPrice = 8m, Notes = "" });
        await _context.SaveChangesAsync();

        var list1 = new List<OrderItemCreateDto> { new OrderItemCreateDto { MenuItemId = 107, Quantity = 1, Notes = "Spicy" } };
        var result1 = await _orderService.AddOrderItemsAsync(113, list1);
        Assert.That(result1.OrderItems.ElementAt(0).Notes, Is.EqualTo("Spicy"));

        // Scenario B: Adding with null/empty notes (should keep existing notes)
        var list2 = new List<OrderItemCreateDto> { new OrderItemCreateDto { MenuItemId = 107, Quantity = 1, Notes = null } };
        var result2 = await _orderService.AddOrderItemsAsync(113, list2);
        Assert.That(result2.OrderItems.ElementAt(0).Notes, Is.EqualTo("Spicy"));
    }

    [Test]
    public async Task AddOrderItemsAsync_TransactionRollback_Throws()
    {
        var mockItemService = new Mock<IItemService>();
        mockItemService.Setup(s => s.UpdateStockByMenuItemIdAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("Stock update failed"));

        var service = new OrderService(_context, _orderRepository, _orderItemRepository, mockItemService.Object, new Mock<ISignalService>().Object, new Mock<ILogger<OrderService>>().Object);

        _context.Tables.Add(new Table { Id = 111, Number = 111, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 114, TableId = 111, Status = OrderStatus.Pending });
        _context.Categories.Add(new Category { Id = 8, Name = "Dessert" });
        _context.MenuItems.Add(new MenuItem { Id = 108, Name = "Ice", Price = 1m, CategoryId = 8, PreparationTime = 1 });
        await _context.SaveChangesAsync();

        var list = new List<OrderItemCreateDto> { new OrderItemCreateDto { MenuItemId = 108, Quantity = 1 } };
        Assert.ThrowsAsync<Exception>(async () => await service.AddOrderItemsAsync(114, list));
    }

    [Test]
    public void UpdateOrderStatusAsync_InvalidStatusEnum_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(async () => await _orderService.UpdateOrderStatusAsync(1, 999)); // 999 invalid status
    }

    [Test]
    public void UpdateOrderStatusAsync_OrderNotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _orderService.UpdateOrderStatusAsync(999, (int)OrderStatus.InPrep));
    }

    [Test]
    public async Task UpdateOrderStatusAsync_SameStatus_ReturnsTrue()
    {
        _context.Tables.Add(new Table { Id = 112, Number = 112, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 115, TableId = 112, Status = OrderStatus.Pending });
        await _context.SaveChangesAsync();

        var result = await _orderService.UpdateOrderStatusAsync(115, (int)OrderStatus.Pending);
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task UpdateOrderStatusAsync_OrderNotFoundOnUpdate_ThrowsNotFoundException()
    {
        var mockOrderRepo = new Mock<IOrderRepository>();
        mockOrderRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new Order { Id = 116, TableId = 1, Status = OrderStatus.Pending });
        mockOrderRepo.Setup(r => r.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<OrderStatus>()))
            .ReturnsAsync(false); // Simulate status update returning false

        var service = new OrderService(_context, mockOrderRepo.Object, _orderItemRepository, _itemService, new Mock<ISignalService>().Object, new Mock<ILogger<OrderService>>().Object);
        Assert.ThrowsAsync<NotFoundException>(async () => await service.UpdateOrderStatusAsync(116, (int)OrderStatus.InPrep));
    }

    [Test]
    public async Task UpdateOrderStatusAsync_SignalRNotificationFailure_Succeeds()
    {
        var mockSignalService = new Mock<ISignalService>();
        mockSignalService.Setup(s => s.NotifyGuestSessionEndedAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("SignalR network issue"));

        var service = new OrderService(_context, _orderRepository, _orderItemRepository, _itemService, mockSignalService.Object, new Mock<ILogger<OrderService>>().Object);

        _context.Tables.Add(new Table { Id = 113, Number = 113, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 117, TableId = 113, Status = OrderStatus.Pending });
        await _context.SaveChangesAsync();

        // Pending -> Cancelled (triggers NotifyGuestSessionEndedAsync which throws)
        var result = await service.UpdateOrderStatusAsync(117, (int)OrderStatus.Cancelled);
        Assert.That(result, Is.True); // Overall transition still succeeds
    }

    [Test]
    public void AssignChefToOrderAsync_ChefNotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _orderService.AssignChefToOrderAsync(1, 999));
    }

    [Test]
    public async Task AssignChefToOrderAsync_ChefInvalidRole_ThrowsNotFoundException()
    {
        if (await _context.Roles.FindAsync(1) == null)
        {
            _context.Roles.Add(new Role { Id = 1, Name = UserRole.Admin });
        }
        _context.Users.Add(new User { Id = 120, Name = "Admin Alice", Email = "admin@example.com", PasswordHash = "hash", RoleId = 1 });
        await _context.SaveChangesAsync();

        Assert.ThrowsAsync<NotFoundException>(async () => await _orderService.AssignChefToOrderAsync(1, 120)); // Admin is not chef
    }

    [Test]
    public async Task AssignChefToOrderAsync_OrderNotFound_ThrowsNotFoundException()
    {
        if (await _context.Roles.FindAsync(3) == null)
        {
            _context.Roles.Add(new Role { Id = 3, Name = UserRole.Chef });
        }
        _context.Users.Add(new User { Id = 121, Name = "Chef Chef", Email = "chef2@example.com", PasswordHash = "hash", RoleId = 3 });
        await _context.SaveChangesAsync();

        Assert.ThrowsAsync<NotFoundException>(async () => await _orderService.AssignChefToOrderAsync(999, 121));
    }

    [Test]
    public async Task AssignChefToOrderAsync_InvalidTransition_ThrowsBusinessRuleException()
    {
        if (await _context.Roles.FindAsync(3) == null)
        {
            _context.Roles.Add(new Role { Id = 3, Name = UserRole.Chef });
        }
        _context.Users.Add(new User { Id = 122, Name = "Chef Chef", Email = "chef3@example.com", PasswordHash = "hash", RoleId = 3 });
        _context.Tables.Add(new Table { Id = 114, Number = 114, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 118, TableId = 114, Status = OrderStatus.Completed }); // Completed -> InPrep invalid
        await _context.SaveChangesAsync();

        Assert.ThrowsAsync<BusinessRuleException>(async () => await _orderService.AssignChefToOrderAsync(118, 122));
    }

    [Test]
    public void AssignWaiterToOrderAsync_WaiterNotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _orderService.AssignWaiterToOrderAsync(1, 999));
    }

    [Test]
    public async Task AssignWaiterToOrderAsync_OrderNotFound_ThrowsNotFoundException()
    {
        _context.Users.Add(new User { Id = 123, Name = "Steve Steve", Email = "steve@example.com", PasswordHash = "hash", RoleId = 5 });
        await _context.SaveChangesAsync();

        Assert.ThrowsAsync<NotFoundException>(async () => await _orderService.AssignWaiterToOrderAsync(999, 123));
    }

    [Test]
    public void GetActiveOrderByTableIdAsync_NoActiveOrder_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _orderService.GetActiveOrderByTableIdAsync(999));
    }

    [Test]
    public async Task GetActiveOrderByTableIdAsync_Success()
    {
        _context.Tables.Add(new Table { Id = 115, Number = 115, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 119, TableId = 115, Status = OrderStatus.InPrep, TotalAmount = 5m });
        _context.Categories.Add(new Category { Id = 9, Name = "Mains" });
        _context.MenuItems.Add(new MenuItem { Id = 109, Name = "Steak", Price = 25m, CategoryId = 9, PreparationTime = 15 });
        _context.OrderItems.Add(new OrderItem { Id = 109, OrderId = 119, MenuItemId = 109, Quantity = 1, UnitPrice = 25m });
        await _context.SaveChangesAsync();

        var result = await _orderService.GetActiveOrderByTableIdAsync(115);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(119));
    }

    [Test]
    public void GetGuestOrderTrackingAsync_NoActiveOrder_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _orderService.GetGuestOrderTrackingAsync(999));
    }

    [Test]
    public async Task SafeNotifyOrderUpdateAsync_SignalRFails_LoggedAndSwallowed()
    {
        var mockSignalService = new Mock<ISignalService>();
        mockSignalService.Setup(s => s.NotifyOrderUpdateAsync(It.IsAny<int>(), It.IsAny<GuestOrderTrackingDto>()))
            .ThrowsAsync(new Exception("Outer failure"));
        mockSignalService.Setup(s => s.NotifyTablesUpdatedAsync())
            .ThrowsAsync(new Exception("Inner failure"));

        var service = new OrderService(_context, _orderRepository, _orderItemRepository, _itemService, mockSignalService.Object, new Mock<ILogger<OrderService>>().Object);

        _context.Tables.Add(new Table { Id = 130, Number = 130, Capacity = 4, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 130, TableId = 130, Status = OrderStatus.Pending });
        _context.Categories.Add(new Category { Id = 10, Name = "Drinks" });
        _context.MenuItems.Add(new MenuItem { Id = 130, Name = "Water", Price = 2m, CategoryId = 10, PreparationTime = 1, IsAvailable = true });
        _context.OrderItems.Add(new OrderItem { Id = 130, OrderId = 130, MenuItemId = 130, Quantity = 1, UnitPrice = 2m });
        await _context.SaveChangesAsync();

        // AssignWaiterToOrderAsync does not trigger safe update but AssignChefToOrder does!
        if (await _context.Roles.FindAsync(3) == null)
        {
            _context.Roles.Add(new Role { Id = 3, Name = UserRole.Chef });
        }
        _context.Users.Add(new User { Id = 130, Name = "Chef", Email = "chef130@example.com", PasswordHash = "hash", RoleId = 3 });
        await _context.SaveChangesAsync();

        var result = await service.AssignChefToOrderAsync(130, 130);
        Assert.That(result, Is.True); // Succeeded despite double SignalR exception
    }
}
