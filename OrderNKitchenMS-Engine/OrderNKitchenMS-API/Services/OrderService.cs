using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderNKitchenMS_API.Data;
using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Models.Enums;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Utils;

using Microsoft.Extensions.Logging;

namespace OrderNKitchenMS_API.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly IItemService _itemService;
    private readonly ILogger<OrderService> _logger;
    private static readonly Dictionary<OrderStatus, List<OrderStatus>> AllowedTransitions = new()
    {
        { OrderStatus.Pending, new List<OrderStatus> { OrderStatus.InPrep, OrderStatus.Cancelled } },
        { OrderStatus.InPrep, new List<OrderStatus> { OrderStatus.Ready, OrderStatus.Cancelled } },
        { OrderStatus.Ready, new List<OrderStatus> { OrderStatus.Served, OrderStatus.Completed, OrderStatus.Cancelled } },
        { OrderStatus.Served, new List<OrderStatus> { OrderStatus.Completed, OrderStatus.Cancelled } },
        { OrderStatus.Completed, new List<OrderStatus>() },
        { OrderStatus.Cancelled, new List<OrderStatus>() }
    };

    public OrderService(
        AppDbContext context, 
        IOrderRepository orderRepository, 
        IOrderItemRepository orderItemRepository, 
        IItemService itemService,
        ILogger<OrderService> logger)
    {
        _context = context;
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _itemService = itemService;
        _logger = logger;
    }

    private async Task<DateTime> CalculateEstimatedReadyAtAsync(DateTime createdAt, List<int> menuItemIds, int excludeOrderId = 0)
    {
        var currentOrderPrepTime = 0;
        if (menuItemIds.Any())
        {
            currentOrderPrepTime = await _context.MenuItems
                .Where(mi => menuItemIds.Contains(mi.Id) && !mi.IsDeleted)
                .Select(mi => (int?)mi.PreparationTime)
                .MaxAsync() ?? 0;
        }

        var activeOrdersAheadCount = await _context.Orders
            .CountAsync(o => (o.Status == OrderStatus.Pending || o.Status == OrderStatus.InPrep) && o.Id != excludeOrderId && o.CreatedAt < createdAt);

        const int averagePrepTimePerOrder = 10;

        return DateTime.UtcNow.AddMinutes((activeOrdersAheadCount * averagePrepTimePerOrder) + currentOrderPrepTime);
    }

    // Creates a new order for a table with the specified items.
    public async Task<OrderDto> CreateOrderAsync(int tableId, int waiterId, OrderCreateDto orderCreateDto)
    {
        _logger.LogInformation("CreateOrderAsync started for TableId: {TableId}, WaiterId: {WaiterId}", tableId, waiterId);
        ValidateCreateDto(orderCreateDto);

        var table = await _context.Tables.FirstOrDefaultAsync(table => table.Id == tableId && !table.IsDeleted);
        if (table == null)
        {
            throw new NotFoundException($"Table with id {tableId} was not found.");
        }

        if (table.Status != TableStatus.Available)
        {
            throw new BusinessRuleException($"Table with id {tableId} is not available.");
        }

        var requestedMenuItemIds = orderCreateDto.OrderItems.Select(orderItem => orderItem.MenuItemId).Distinct().ToList();
        var menuItems = await _context.MenuItems
            .Where(m => requestedMenuItemIds.Contains(m.Id) && !m.IsDeleted)
            .ToListAsync();

        await _itemService.ValidateStockForOrderItemsAsync(orderCreateDto.OrderItems);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            table.Status = TableStatus.Occupied;

            var createdAt = DateTime.UtcNow;

            var orderEntity = new Order
            {
                TableId = tableId,
                Status = OrderStatus.Pending,
                TotalAmount = 0m,
                AssignedWaiterId = waiterId,
                CreatedAt = createdAt
            };

            var totalAmount = 0m;
            var orderItemEntities = new List<OrderItem>();

            foreach (var orderItemDto in orderCreateDto.OrderItems)
            {
                var menuItem = menuItems.First(mi => mi.Id == orderItemDto.MenuItemId);
                totalAmount += menuItem.Price * orderItemDto.Quantity;
            }

            orderEntity.TotalAmount = totalAmount;

            await _orderRepository.AddAsync(orderEntity);
            await _context.SaveChangesAsync();

            foreach (var orderItemDto in orderCreateDto.OrderItems)
            {
                var menuItem = menuItems.First(mi => mi.Id == orderItemDto.MenuItemId);
                var unitPrice = menuItem.Price;

                // Deduct stock
                await _itemService.UpdateStockByMenuItemIdAsync(orderItemDto.MenuItemId, orderItemDto.Quantity, false);

                var createdOrderItem = await _orderItemRepository.AddAsync(new OrderItem
                {
                    OrderId = orderEntity.Id,
                    MenuItemId = menuItem.Id,
                    Quantity = orderItemDto.Quantity,
                    UnitPrice = unitPrice,
                    Notes = orderItemDto.Notes?.Trim() ?? string.Empty
                });

                orderItemEntities.Add(createdOrderItem);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("CreateOrderAsync succeeded. Created Order ID: {OrderId} with Total Amount: {TotalAmount}", orderEntity.Id, orderEntity.TotalAmount);
            
            orderEntity.AssignedWaiter = await _context.Users.FindAsync(waiterId);

            return await MapOrderToDtoAsync(orderEntity, table, orderItemEntities, menuItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateOrderAsync failed for TableId: {TableId}. Transaction rolled back.", tableId);
            await transaction.RollbackAsync();
            throw;
        }
    }

    // public async Task<IEnumerable<OrderDto>> GetActiveOrdersAsync()
    // {
    //     var orders = await _orderRepository.GetAllAsync();
    //     var activeOrders = orders.Where(order => order.Status != OrderStatus.Completed && order.Status != OrderStatus.Cancelled).ToList();

    // }

    // Retrieves a specific order details by its unique identifier.
    public async Task<OrderDto> GetOrderByIdAsync(int id)
    {
        _logger.LogInformation("GetOrderByIdAsync called for Order ID: {Id}", id);
        var order = await _orderRepository.GetByIdAsync(id);
        if(order == null)
        {
            _logger.LogWarning("GetOrderByIdAsync failed: Order with ID {Id} was not found", id);
            throw new NotFoundException($"Order with id {id} was not found");
        }

        var table = order.Table;

        var orderItems = await _orderItemRepository.GetByOrderIdAsync(order.Id);
        var menuItems = orderItems.Select(oi => oi.MenuItem).ToList();

        return await MapOrderToDtoAsync(order, table, orderItems, menuItems);
    }

    // Retrieves all orders filtered by query parameters.
    public async Task<IEnumerable<OrderDto>> GetAllOrdersAsync(QueryOrderDto query)
    {
        _logger.LogInformation("GetAllOrdersAsync called. Status: {Status}, TableId: {TableId}", query?.Status, query?.TableId);
        var orders = await _orderRepository.GetAllAsync();

        if (query.Status.HasValue)
        {
            orders = orders.Where(o => (int)o.Status == query.Status.Value);
        }

        if (query.TableId.HasValue)
        {
            orders = orders.Where(o => o.TableId == query.TableId.Value);
        }

        if (query.From.HasValue)
        {
            orders = orders.Where(o => o.CreatedAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            orders = orders.Where(o => o.CreatedAt <= query.To.Value);
        }

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        var paginatedOrders = orders
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new List<OrderDto>();
        foreach (var order in paginatedOrders)
        {
            var orderItems = await _orderItemRepository.GetByOrderIdAsync(order.Id);
            var menuItems = orderItems.Select(oi => oi.MenuItem).ToList();
            result.Add(await MapOrderToDtoAsync(order, order.Table, orderItems, menuItems));
        }

        return result;
    }

    // Retrieves all active orders (neither completed nor cancelled).
    public async Task<IEnumerable<OrderDto>> GetActiveOrdersAsync()
    {
        _logger.LogInformation("GetActiveOrdersAsync called");
        var orders = await _orderRepository.GetAllAsync();
        var activeOrders = orders
            .Where(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        var result = new List<OrderDto>();
        foreach (var order in activeOrders)
        {
            var orderItems = await _orderItemRepository.GetByOrderIdAsync(order.Id);
            var menuItems = orderItems.Select(oi => oi.MenuItem).ToList();
            result.Add(await MapOrderToDtoAsync(order, order.Table, orderItems, menuItems));
        }

        return result;
    }

    // Removes a specific item from a pending order.
    public async Task<bool> RemoveOrderItemAsync(int orderId, int orderItemId)
    {
        _logger.LogInformation("RemoveOrderItemAsync started. OrderId: {OrderId}, OrderItemId: {OrderItemId}", orderId, orderItemId);
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new NotFoundException($"Order with id {orderId} was not found.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            throw new BusinessRuleException($"Cannot remove items from an order with status '{order.Status}'. Items can only be removed from Pending orders.");
        }

        var orderItem = await _orderItemRepository.GetByIdAsync(orderItemId);
        if (orderItem == null || orderItem.OrderId != orderId)
        {
            throw new NotFoundException($"Order item with id {orderItemId} was not found on order {orderId}.");
        }

        var itemTotalAmount = orderItem.UnitPrice * orderItem.Quantity;
        order.TotalAmount = Math.Max(0m, order.TotalAmount - itemTotalAmount);

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Restore stock
            await _itemService.UpdateStockByMenuItemIdAsync(orderItem.MenuItemId, orderItem.Quantity, true);

            // Delete order item
            await _orderItemRepository.DeleteAsync(orderItemId);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }

        return true;
    }

    // Adds new items to an existing order.
    public async Task<OrderDto> AddOrderItemsAsync(int orderId, List<OrderItemCreateDto> orderItemCreateDtos)
    {
        _logger.LogInformation("AddOrderItemsAsync started. OrderId: {OrderId}, adding {Count} items.", orderId, orderItemCreateDtos?.Count);
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            throw new NotFoundException($"Order with id {orderId} was not found.");
        }

        if (order.Status != OrderStatus.Pending)
        {
            throw new BusinessRuleException($"Cannot add items to an order with status '{order.Status}'. Items can only be added to Pending orders.");
        }

        var addedMenuItemIds = orderItemCreateDtos.Select(o => o.MenuItemId).Distinct().ToList();
        var menuItems = await _context.MenuItems
            .Where(m => addedMenuItemIds.Contains(m.Id) && !m.IsDeleted)
            .ToListAsync();

        await _itemService.ValidateStockForOrderItemsAsync(orderItemCreateDtos);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            foreach (var orderItemCreateDto in orderItemCreateDtos)
            {
                var menuItem = menuItems.First(mi => mi.Id == orderItemCreateDto.MenuItemId);
                var unitPrice = menuItem.Price;
                var totalPrice = unitPrice * orderItemCreateDto.Quantity;

                // Deduct stock
                await _itemService.UpdateStockByMenuItemIdAsync(orderItemCreateDto.MenuItemId, orderItemCreateDto.Quantity, false);

                var orderItemEntity = new OrderItem
                {
                    OrderId = orderId,
                    MenuItemId = orderItemCreateDto.MenuItemId,
                    Quantity = orderItemCreateDto.Quantity,
                    UnitPrice = unitPrice,
                    Notes = orderItemCreateDto.Notes?.Trim() ?? string.Empty
                };

                await _orderRepository.AddMenuItemAsync(orderId, orderItemEntity);

                order.TotalAmount += totalPrice;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }

        var table = order.Table;

        var orderItems = await _orderItemRepository.GetByOrderIdAsync(order.Id);
        var orderMenuItems = orderItems.Select(oi => oi.MenuItem).ToList();

        return await MapOrderToDtoAsync(order, table, orderItems, orderMenuItems);
    }

    // Updates the status of an existing order.
    public async Task<bool> UpdateOrderStatusAsync(int orderId, int status)
    {
        _logger.LogInformation("UpdateOrderStatusAsync started. OrderId: {OrderId}, new status: {Status}", orderId, status);
        EnsureValidStatus(status);
        
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            _logger.LogWarning("UpdateOrderStatusAsync failed: Order with ID {OrderId} was not found.", orderId);
            throw new NotFoundException($"Order with id {orderId} was not found.");
        }

        var newStatus = (OrderStatus)status;
        var oldStatus = order.Status;

        if (newStatus == oldStatus)
        {
            return true;
        }

        if (!AllowedTransitions.TryGetValue(oldStatus, out var allowed) || !allowed.Contains(newStatus))
        {
            throw new BusinessRuleException($"Cannot transition order status from '{oldStatus}' to '{newStatus}'.");
        }

        if (newStatus == OrderStatus.Cancelled)
        {
            var orderItems = await _orderItemRepository.GetByOrderIdAsync(orderId);
            foreach (var orderItem in orderItems)
            {
                await _itemService.UpdateStockByMenuItemIdAsync(orderItem.MenuItemId, orderItem.Quantity, true);
            }
        }

        var isChanged = await _orderRepository.UpdateStatusAsync(orderId, newStatus);
        _logger.LogInformation("UpdateOrderStatusAsync succeeded. OrderId: {OrderId} status changed to: {Status}", orderId, newStatus);
        return true;
    }

    // Assigns an order to a specific chef and sets status to InPrep.
    public async Task<bool> AssignChefToOrderAsync(int orderId, int chefId)
    {
        _logger.LogInformation("AssignChefToOrderAsync started. OrderId: {OrderId}, ChefId: {ChefId}", orderId, chefId);
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == chefId && !user.IsDeleted);
        if (user == null || user.RoleId != 3) // Chef role is 3
        {
            _logger.LogWarning("AssignChefToOrderAsync failed: Chef with ID {ChefId} was not found or invalid role.", chefId);
            throw new NotFoundException($"Chef with id {chefId} was not found.");
        }

        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            _logger.LogWarning("AssignChefToOrderAsync failed: Order with ID {OrderId} was not found.", orderId);
            throw new NotFoundException($"Order with id {orderId} was not found.");
        }

        var oldStatus = order.Status;
        var newStatus = OrderStatus.InPrep;

        if (oldStatus != newStatus)
        {
            if (!AllowedTransitions.TryGetValue(oldStatus, out var allowed) || !allowed.Contains(newStatus))
            {
                throw new BusinessRuleException($"Cannot transition order status from '{oldStatus}' to '{newStatus}'.");
            }
            order.Status = newStatus;
        }

        order.AssignedChefId = chefId;
        await _context.SaveChangesAsync();
        _logger.LogInformation("AssignChefToOrderAsync succeeded. OrderId: {OrderId} assigned to ChefId: {ChefId}", orderId, chefId);
        return true;
    }

    // Assigns a waiter to an order.
    public async Task<bool> AssignWaiterToOrderAsync(int orderId, int waiterId)
    {
        _logger.LogInformation("AssignWaiterToOrderAsync started. OrderId: {OrderId}, WaiterId: {WaiterId}", orderId, waiterId);
        var user = await _context.Users.FirstOrDefaultAsync(user => user.Id == waiterId && !user.IsDeleted);
        if (user == null)
        {
            _logger.LogWarning("AssignWaiterToOrderAsync failed: Waiter with ID {WaiterId} was not found.", waiterId);
            throw new NotFoundException($"Waiter with id {waiterId} was not found.");
        }

        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            _logger.LogWarning("AssignWaiterToOrderAsync failed: Order with ID {OrderId} was not found.", orderId);
            throw new NotFoundException($"Order with id {orderId} was not found.");
        }

        order.AssignedWaiterId = waiterId;
        await _context.SaveChangesAsync();
        _logger.LogInformation("AssignWaiterToOrderAsync succeeded. OrderId: {OrderId} assigned to WaiterId: {WaiterId}", orderId, waiterId);
        return true;
    }

    // Retrieves the active order (neither completed nor cancelled) for a specific table.
    public async Task<OrderDto> GetActiveOrderByTableIdAsync(int tableId)
    {
        _logger.LogInformation("GetActiveOrderByTableIdAsync called for TableId: {TableId}", tableId);
        var orders = await _orderRepository.GetAllAsync();
        var activeOrder = orders
            .Where(o => o.TableId == tableId && o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefault();

        if (activeOrder == null)
        {
            _logger.LogWarning("GetActiveOrderByTableIdAsync failed: No active order found for TableId: {TableId}", tableId);
            throw new NotFoundException($"No active order found for table with ID {tableId}.");
        }

        var orderItems = await _orderItemRepository.GetByOrderIdAsync(activeOrder.Id);
        var menuItems = orderItems.Select(oi => oi.MenuItem).ToList();

        _logger.LogInformation("GetActiveOrderByTableIdAsync succeeded. Active Order ID: {OrderId} found for TableId: {TableId}", activeOrder.Id, tableId);
        return await MapOrderToDtoAsync(activeOrder, activeOrder.Table, orderItems, menuItems);
    }

    // Retrieves live food tracking details for the guest session.
    public async Task<GuestOrderTrackingDto> GetGuestOrderTrackingAsync(int tableId)
    {
        _logger.LogInformation("GetGuestOrderTrackingAsync called for TableId: {TableId}", tableId);
        var orders = await _orderRepository.GetAllAsync();
        var activeOrder = orders
            .Where(o => o.TableId == tableId && o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefault();

        if (activeOrder == null)
        {
            _logger.LogWarning("GetGuestOrderTrackingAsync failed: No active order found for TableId: {TableId}", tableId);
            throw new NotFoundException($"No active order found for table with ID {tableId}.");
        }

        var orderItems = await _orderItemRepository.GetByOrderIdAsync(activeOrder.Id);
        var menuItems = orderItems.Select(oi => oi.MenuItem).ToList();

        var queuePosition = 0;
        var estimatedTimeMinutes = 0;
        DateTime? estimatedReadyAt = null;

        if (activeOrder.Status == OrderStatus.Pending || activeOrder.Status == OrderStatus.InPrep)
        {
            var activeOrders = orders
                .Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.InPrep)
                .OrderBy(o => o.CreatedAt)
                .ToList();

            var index = activeOrders.FindIndex(o => o.Id == activeOrder.Id);
            queuePosition = index >= 0 ? index + 1 : 0;

            var menuItemIds = orderItems.Select(oi => oi.MenuItemId).ToList();
            estimatedReadyAt = await CalculateEstimatedReadyAtAsync(activeOrder.CreatedAt, menuItemIds, activeOrder.Id);
            estimatedTimeMinutes = Math.Max(0, (int)Math.Ceiling((estimatedReadyAt.Value - DateTime.UtcNow).TotalMinutes));
        }

        return new GuestOrderTrackingDto
        {
            OrderId = activeOrder.Id,
            TableId = activeOrder.TableId,
            Status = activeOrder.Status.ToString(),
            QueuePosition = queuePosition,
            EstimatedReadyAt = estimatedReadyAt,
            EstimatedTimeMinutes = estimatedTimeMinutes,
            OrderItems = orderItems.Select(oi => new OrderItemTrackingDto
            {
                MenuItemName = menuItems.First(mi => mi.Id == oi.MenuItemId).Name,
                Quantity = oi.Quantity,
                Notes = oi.Notes
            }).ToArray()
        };
    }

    private static void ValidateCreateDto(OrderCreateDto orderCreateDto)
    {
        Validation.RequireNotNull(orderCreateDto, nameof(orderCreateDto), "Order data is required.");
        Validation.Require(orderCreateDto.OrderItems != null && orderCreateDto.OrderItems.Count > 0, "At least one order item is required.", nameof(orderCreateDto.OrderItems));
        Validation.Require(!orderCreateDto.OrderItems!.Any(orderItem => orderItem == null), "Order items cannot contain null entries.", nameof(orderCreateDto.OrderItems));
    }

    private static void EnsureValidStatus(int status)
    {
        Validation.RequireValidEnum<OrderStatus>(status, nameof(status), "Invalid order status.");
    }

    private async Task<OrderDto> MapOrderToDtoAsync(
        Order order,
        Table table,
        IEnumerable<OrderItem> orderItems,
        IReadOnlyList<MenuItem> menuItems)
    {
        var menuItemIds = orderItems.Select(oi => oi.MenuItemId).ToList();
        var estimatedReadyAt = await CalculateEstimatedReadyAtAsync(order.CreatedAt, menuItemIds, order.Id);

        return new OrderDto
        {
            Id = order.Id,
            TableId = order.TableId,
            TableNumber = table?.Number ?? 0,
            Status = (int)order.Status,
            StatusName = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            CompletedAt = order.CompletedAt,
            CreatedAt = order.CreatedAt,
            AssignedChefId = order.AssignedChefId,
            AssignedChefName = order.AssignedChef?.Name,
            AssignedWaiterId = order.AssignedWaiterId,
            AssignedWaiterName = order.AssignedWaiter?.Name,
            EstimatedReadyAt = estimatedReadyAt,
            OrderItems = orderItems.Select(orderItem => MapOrderItemToDto(orderItem, menuItems.First(mi => mi.Id == orderItem.MenuItemId).Name)).ToArray()
        };
    }

    private static OrderItemDto MapOrderItemToDto(OrderItem orderItem, string menuItemName)
    {
        return new OrderItemDto
        {
            Id = orderItem.Id,
            OrderId = orderItem.OrderId,
            MenuItemId = orderItem.MenuItemId,
            MenuItemName = menuItemName,
            Quantity = orderItem.Quantity,
            UnitPrice = orderItem.UnitPrice,
            Notes = orderItem.Notes,
            CreatedAt = orderItem.CreatedAt
        };
    }
}
