using System;
using Microsoft.AspNetCore.SignalR;
using OrderNKitchenMS_API.Hubs;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Services.Interfaces;

namespace OrderNKitchenMS_API.Services;

public class SignalService : ISignalService
{
    private readonly IHubContext<RestaurantHub> _hubContext;
    private readonly ILogger<SignalService> _logger;

    public SignalService(IHubContext<RestaurantHub> hubContext, ILogger<SignalService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyNewOrderAsync(OrderDto orderDto)
    {
        _logger.LogInformation("Notifying kitchen about new order: {OrderId}", orderDto.Id);

        await _hubContext.Clients.Group("kitchen")
            .SendAsync("ReceiveNewOrder", orderDto);
    }

    public async Task NotifyOrderUpdateAsync(int tableId, GuestOrderTrackingDto trackingDto)
    {
        _logger.LogInformation("Notifying table-{TableId} and kitchen about order update for Order: {OrderId}", tableId, trackingDto.OrderId);

        await _hubContext.Clients.Group($"table-{tableId}")
            .SendAsync("ReceiveOrderUpdate", trackingDto);

        await _hubContext.Clients.Group("kitchen")
            .SendAsync("ReceiveOrderUpdate", trackingDto);
    }

    public async Task NotifyTablesUpdatedAsync()
    {
        _logger.LogInformation("Broadcasting table state update to all clients");
        await _hubContext.Clients.All.SendAsync("ReceiveTableStateUpdate");
    }

    public async Task NotifyBillGeneratedAsync(int tableId, BillDto billDto)
    {
        _logger.LogInformation("Notifying table-{TableId} about bill generated: {BillId}", tableId, billDto.Id);
        await _hubContext.Clients.Group($"table-{tableId}")
            .SendAsync("bill_generated", billDto);
    }

    public async Task NotifyBillPaidAsync(int tableId, BillDto billDto)
    {
        _logger.LogInformation("Notifying table-{TableId} about bill paid: {BillId}", tableId, billDto.Id);
        await _hubContext.Clients.Group($"table-{tableId}")
            .SendAsync("bill_paid", billDto);
    }

    public async Task NotifyLowStockAlertAsync(string chefName, int itemId, string itemName, decimal currentStock, string unitName)
    {
        var message = $"[STOCK ALERT] Chef {chefName} flagged low stock for {itemName}: {currentStock} {unitName} remaining.";
        _logger.LogInformation("Broadcasting stock warning to admin group for item: {ItemName}", itemName);

        await _hubContext.Clients.Group("admins").SendAsync("ReceiveAdminAlert", new
        {
            itemId,
            itemName,
            stockQuantity = currentStock,
            unitName,
            message,
            flaggedBy = chefName,
            createdAt = DateTime.UtcNow
        });
    }
}
