using System;
using System.Threading.Tasks;
using OrderNKitchenMS_API.Models.DTOs;

namespace OrderNKitchenMS_API.Services.Interfaces;

public interface ISignalService
{
    //Chef signals
    public Task NotifyNewOrderAsync(OrderDto orderDto);

    //Guest signals
    public Task NotifyOrderUpdateAsync(int tableId, GuestOrderTrackingDto trackingDto);

    //Waiter/Generic tables updates
    public Task NotifyTablesUpdatedAsync();
}

