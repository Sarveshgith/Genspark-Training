// @feature Backend API | Order Management | Handles creation of new orders, updating items, checkout billing, and preparation statuses.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Models.Enums;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Utils;
using Microsoft.Extensions.Logging;

namespace OrderNKitchenMS_API.Controllers;

[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(IOrderService orderService, ILogger<OrderController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [Authorize(Policy = "AllStaff")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders([FromQuery] QueryOrderDto query)
    {
        _logger.LogInformation("GetOrders requested. TableId: {TableId}, Status: {Status}", query?.TableId, query?.Status);
        var orders = await _orderService.GetAllOrdersAsync(query ?? new QueryOrderDto());
        _logger.LogInformation("GetOrders completed. Returned {Count} orders.", orders.Count());
        return Ok(orders);
    }

    [Authorize(Policy = "AllStaff")]
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetActiveOrders()
    {
        _logger.LogInformation("GetActiveOrders requested");
        var orders = await _orderService.GetActiveOrdersAsync();
        _logger.LogInformation("GetActiveOrders completed. Returned {Count} active orders.", orders.Count());
        return Ok(orders);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrderById(int id)
    {
        Validation.ValidateId(id);
        _logger.LogInformation("GetOrderById requested for ID: {Id}", id);
        var order = await _orderService.GetOrderByIdAsync(id);
        _logger.LogInformation("GetOrderById completed for ID: {Id}", id);
        return Ok(order);
    }

    [Authorize(Policy = "CanPlaceOrder")]
    [HttpGet("me")]
    public async Task<ActionResult<OrderDto>> GetMyOrder()
    {
        var tableId = User.GetTableId();
        _logger.LogInformation("GetMyOrder requested for TableId from Claims: {TableId}", tableId);
        if (!tableId.HasValue)
        {
            _logger.LogWarning("GetMyOrder failed: TableId claim is missing.");
            throw new BusinessRuleException("Table ID is required to fetch active order.");
        }
        var order = await _orderService.GetActiveOrderByTableIdAsync(tableId.Value);
        _logger.LogInformation("GetMyOrder completed for TableId: {TableId}. Order ID: {OrderId}", tableId.Value, order.Id);
        return Ok(order);
    }

    [Authorize(Policy = "CanPlaceOrder")]
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] OrderCreateDto orderCreateDto)
    {
        Validation.RequireNotNull(orderCreateDto, nameof(orderCreateDto));
        var waiterId = User.GetUserId();
        var tableId = orderCreateDto.TableId;
        _logger.LogInformation("CreateOrder requested for TableId: {TableId} by WaiterId: {WaiterId}", tableId, waiterId);
        if (tableId <= 0)
        {
            _logger.LogWarning("CreateOrder failed: TableId is missing or invalid.");
            throw new BusinessRuleException("Table ID is required to create an order.");
        }
        var createdOrder = await _orderService.CreateOrderAsync(tableId, waiterId, orderCreateDto);
        _logger.LogInformation("CreateOrder completed. Created Order ID: {OrderId} for TableId: {TableId}", createdOrder.Id, tableId);
        return CreatedAtAction(nameof(GetOrderById), new { id = createdOrder.Id }, createdOrder);
    }

    [Authorize(Policy = "CanPlaceOrder")]
    [HttpPost("{orderId:int}/items")]
    public async Task<ActionResult<OrderDto>> AddOrderItems(int orderId, [FromBody] List<OrderItemCreateDto> orderItemCreateDtos)
    {
        Validation.ValidateRequest(orderId, orderItemCreateDtos, nameof(orderId), nameof(orderItemCreateDtos));
        Validation.Require(orderItemCreateDtos.Any(), "Order item data is required.", nameof(orderItemCreateDtos));

        _logger.LogInformation("AddOrderItems requested for OrderId: {OrderId}, adding {Count} items.", orderId, orderItemCreateDtos.Count);
        var updatedOrder = await _orderService.AddOrderItemsAsync(orderId, orderItemCreateDtos);
        _logger.LogInformation("AddOrderItems completed for OrderId: {OrderId}. Updated total amount: {TotalAmount}", orderId, updatedOrder.TotalAmount);
        return Ok(updatedOrder);
    }

    [Authorize(Policy = "CanPlaceOrder")]
    [HttpDelete("{orderId:int}/items/{itemId:int}")]
    public async Task<ActionResult> RemoveOrderItem(int orderId, int itemId)
    {
        Validation.ValidateId(orderId, nameof(orderId));
        Validation.ValidateId(itemId, nameof(itemId));

        _logger.LogInformation("RemoveOrderItem requested for OrderId: {OrderId}, ItemId: {ItemId}", orderId, itemId);
        await _orderService.RemoveOrderItemAsync(orderId, itemId);
        _logger.LogInformation("RemoveOrderItem completed for OrderId: {OrderId}, ItemId: {ItemId}", orderId, itemId);
        return NoContent();
    }

    [Authorize(Policy = "AllStaff")]
    [HttpPatch("{orderId:int}/status")]
    public async Task<ActionResult> UpdateOrderStatus(int orderId, [FromBody] int status)
    {
        Validation.ValidateId(orderId, nameof(orderId));
        Validation.RequireValidEnum<OrderStatus>(status, nameof(status));

        _logger.LogInformation("UpdateOrderStatus requested for OrderId: {OrderId}, new status: {Status}", orderId, status);
        await _orderService.UpdateOrderStatusAsync(orderId, status);
        _logger.LogInformation("UpdateOrderStatus completed for OrderId: {OrderId}", orderId);
        return NoContent();
    }

    [Authorize(Policy = "AdminOrChef")]
    [HttpPatch("{orderId:int}/assign-chef")]
    public async Task<ActionResult> AssignChef(int orderId)
    {
        Validation.ValidateId(orderId, nameof(orderId));
        var chefId = User.GetUserId();
        _logger.LogInformation("AssignChef requested for OrderId: {OrderId} by ChefId: {ChefId}", orderId, chefId);
        await _orderService.AssignChefToOrderAsync(orderId, chefId);
        _logger.LogInformation("AssignChef completed for OrderId: {OrderId}", orderId);
        return NoContent();
    }

    [Authorize(Policy = "CanPlaceOrder")]
    [HttpPatch("{orderId:int}/assign-waiter")]
    public async Task<ActionResult> AssignWaiter(int orderId)
    {
        Validation.ValidateId(orderId, nameof(orderId));
        var waiterId = User.GetUserId();
        _logger.LogInformation("AssignWaiter requested for OrderId: {OrderId} by WaiterId: {WaiterId}", orderId, waiterId);
        await _orderService.AssignWaiterToOrderAsync(orderId, waiterId);
        _logger.LogInformation("AssignWaiter completed for OrderId: {OrderId}", orderId);
        return NoContent();
    }

    [Authorize(Policy = "GuestSession")]
    [HttpGet("track")]
    public async Task<ActionResult<GuestOrderTrackingDto>> TrackMyOrder()
    {
        var tableId = User.GetTableId();
        _logger.LogInformation("TrackMyOrder requested for TableId from Claims: {TableId}", tableId);
        if (!tableId.HasValue)
        {
            _logger.LogWarning("TrackMyOrder failed: TableId claim is missing.");
            throw new BusinessRuleException("Table ID is required to track order.");
        }
        var trackingInfo = await _orderService.GetGuestOrderTrackingAsync(tableId.Value);
        return Ok(trackingInfo);
    }

    [Authorize(Policy = "AllStaff")]
    [HttpGet("table/{tableId:int}/active")]
    public async Task<ActionResult<OrderDto>> GetActiveOrderByTableId(int tableId)
    {
        Validation.ValidateId(tableId, nameof(tableId));
        _logger.LogInformation("GetActiveOrderByTableId requested for TableId: {TableId}", tableId);
        var order = await _orderService.GetActiveOrderByTableIdAsync(tableId);
        _logger.LogInformation("GetActiveOrderByTableId completed for TableId: {TableId}. Order ID: {OrderId}", tableId, order.Id);
        return Ok(order);
    }
}