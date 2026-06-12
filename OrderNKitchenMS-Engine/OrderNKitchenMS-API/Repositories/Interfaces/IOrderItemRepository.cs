using OrderNKitchenMS_API.Models.Entities;

namespace OrderNKitchenMS_API.Repositories.Interfaces;

public interface IOrderItemRepository
{
    public Task<IEnumerable<OrderItem>> GetAllAsync();

    public Task<OrderItem?> GetByIdAsync(int id);

    public Task<IEnumerable<OrderItem>> GetByOrderIdAsync(int orderId);

    public Task<OrderItem> AddAsync(OrderItem orderItem);

    public Task<OrderItem?> UpdateAsync(int id, OrderItem orderItem);

    public Task<bool> DeleteAsync(int id);
}