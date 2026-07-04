using System.Collections.Generic;
using System.Threading.Tasks;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Models.Enums;

namespace OrderNKitchenMS_API.Repositories.Interfaces;

public interface IOrderRepository
{
    public Task<IEnumerable<Order>> GetAllAsync();

    public Task<Order?> GetByIdAsync(int id);

    public Task<Order> AddAsync(Order order);

    public Task<Order?> UpdateAsync(int id, Order order);

    public Task<Order?> AddMenuItemAsync(int orderId, OrderItem orderItem);

    public Task<bool> UpdateStatusAsync(int id, OrderStatus status);

    public Task<bool> CancelAsync(int id);
}
