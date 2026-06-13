using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrderNKitchenMS_API.Models.DTOs;

namespace OrderNKitchenMS_API.Services.Interfaces;

public interface IOrderService
{
    public Task<OrderDto> CreateOrderAsync(int tableId, int waiterId, OrderCreateDto orderCreateDto);

    public Task<OrderDto> AddOrderItemsAsync(int orderId, List<OrderItemCreateDto> orderItemCreateDtos);

    public Task<OrderDto> GetOrderByIdAsync(int id);

    public Task<bool> UpdateOrderStatusAsync(int orderId, int newStatus);

    public Task<bool> AssignChefToOrderAsync(int orderId, int chefId);
    
    public Task<bool> AssignWaiterToOrderAsync(int orderId, int waiterId);

    public Task<IEnumerable<OrderDto>> GetAllOrdersAsync(QueryOrderDto query);

    public Task<IEnumerable<OrderDto>> GetActiveOrdersAsync();

    public Task<bool> RemoveOrderItemAsync(int orderId, int orderItemId);

    public Task<OrderDto> GetActiveOrderByTableIdAsync(int tableId);

    public Task<GuestOrderTrackingDto> GetGuestOrderTrackingAsync(int tableId);
}
