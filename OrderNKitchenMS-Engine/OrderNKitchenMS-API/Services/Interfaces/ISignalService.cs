using System;
using OrderNKitchenMS_API.Models.DTOs;

namespace OrderNKitchenMS_API.Services.Interfaces;

public interface ISignalService
{
    //Chef signals
    public Task NotifyNewOrderAsync(OrderDto orderDto);

}
