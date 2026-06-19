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
}
