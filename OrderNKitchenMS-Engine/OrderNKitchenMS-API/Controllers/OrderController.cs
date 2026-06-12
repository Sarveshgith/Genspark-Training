using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.DTOs;
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

    [Authorize(Policy = "AdminOnly")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders([FromQuery] QueryOrderDto query)
    {
        _logger.LogInformation("GetOrders requested. TableId: {TableId}, Status: {Status}", query?.TableId, query?.Status);
        var orders = await _orderService.GetAllOrdersAsync(query ?? new QueryOrderDto());
        _logger.LogInformation("GetOrders completed. Returned {Count} orders.", orders.Count());
        return Ok(orders);
    }

    [Authorize(Policy = "AdminOrChef")]
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
        var tableId = User.GetTableId();
        _logger.LogInformation("CreateOrder requested for TableId from Claims: {TableId}", tableId);
        if (!tableId.HasValue)
        {
            _logger.LogWarning("CreateOrder failed: TableId claim is missing.");
            throw new BusinessRuleException("Table ID is required to create an order.");
        }
        var createdOrder = await _orderService.CreateOrderAsync(tableId.Value, orderCreateDto);
        _logger.LogInformation("CreateOrder completed. Created Order ID: {OrderId} for TableId: {TableId}", createdOrder.Id, tableId.Value);
        return Ok(createdOrder);
    }

    [Authorize(Policy = "CanPlaceOrder")]
    [HttpPost("{orderId}/items")]
    public async Task<ActionResult<OrderDto>> AddOrderItems(int orderId, [FromBody] List<OrderItemCreateDto> orderItemCreateDtos)
    {
        _logger.LogInformation("AddOrderItems requested for OrderId: {OrderId}, adding {Count} items.", orderId, orderItemCreateDtos.Count);
        Validation.RequireNotNull(orderItemCreateDtos, nameof(orderItemCreateDtos), "Order item data is required.");
        Validation.Require(orderItemCreateDtos.Any(), "Order item data is required.", nameof(orderItemCreateDtos));

        var updatedOrder = await _orderService.AddOrderItemsAsync(orderId, orderItemCreateDtos);
        _logger.LogInformation("AddOrderItems completed for OrderId: {OrderId}. Updated total amount: {TotalAmount}", orderId, updatedOrder.TotalAmount);
        return Ok(updatedOrder);
    }

    [Authorize(Policy = "CanPlaceOrder")]
    [HttpDelete("{orderId}/items/{itemId}")]
    public async Task<ActionResult> RemoveOrderItem(int orderId, int itemId)
    {
        _logger.LogInformation("RemoveOrderItem requested for OrderId: {OrderId}, ItemId: {ItemId}", orderId, itemId);
        await _orderService.RemoveOrderItemAsync(orderId, itemId);
        _logger.LogInformation("RemoveOrderItem completed for OrderId: {OrderId}, ItemId: {ItemId}", orderId, itemId);
        return NoContent();
    }

    [Authorize(Policy = "AdminOrChef")]
    [HttpPatch("{orderId}/status")]
    public async Task<ActionResult> UpdateOrderStatus(int orderId, [FromBody] int status)
    {
        _logger.LogInformation("UpdateOrderStatus requested for OrderId: {OrderId}, new status: {Status}", orderId, status);
        await _orderService.UpdateOrderStatusAsync(orderId, status);
        _logger.LogInformation("UpdateOrderStatus completed for OrderId: {OrderId}", orderId);
        return NoContent();
    }

    [Authorize(Policy = "AdminOrChef")]
    [HttpPatch("{orderId}/assign")]
    public async Task<ActionResult> AssignOrder(int orderId, [FromBody] int userId)
    {
        _logger.LogInformation("AssignOrder requested for OrderId: {OrderId}, assigning to UserId: {UserId}", orderId, userId);
        await _orderService.AssignOrderToUserAsync(orderId, userId);
        _logger.LogInformation("AssignOrder completed for OrderId: {OrderId}", orderId);
        return NoContent();
    }
}