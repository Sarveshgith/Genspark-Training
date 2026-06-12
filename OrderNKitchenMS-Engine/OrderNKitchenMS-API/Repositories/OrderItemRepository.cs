using Microsoft.EntityFrameworkCore;
using OrderNKitchenMS_API.Data;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Repositories.Interfaces;

namespace OrderNKitchenMS_API.Repositories;

public class OrderItemRepository : IOrderItemRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<OrderItem> _orderItems;

    public OrderItemRepository(AppDbContext context)
    {
        _context = context;
        _orderItems = _context.OrderItems;
    }

    public async Task<IEnumerable<OrderItem>> GetAllAsync()
    {
        return await _orderItems
            .Include(orderItem => orderItem.Order)
            .Include(orderItem => orderItem.MenuItem)
            .ToListAsync();
    }

    public async Task<OrderItem?> GetByIdAsync(int id)
    {
        return await _orderItems
            .Include(orderItem => orderItem.Order)
            .Include(orderItem => orderItem.MenuItem)
            .FirstOrDefaultAsync(orderItem => orderItem.Id == id);
    }

    public async Task<IEnumerable<OrderItem>> GetByOrderIdAsync(int orderId)
    {
        return await _orderItems
            .Include(orderItem => orderItem.MenuItem)
            .Where(orderItem => orderItem.OrderId == orderId)
            .ToListAsync();
    }

    public async Task<OrderItem> AddAsync(OrderItem orderItem)
    {
        _orderItems.Add(orderItem);
        return orderItem;
    }

    public async Task<OrderItem?> UpdateAsync(int id, OrderItem orderItem)
    {
        var existingOrderItem = await GetByIdAsync(id);
        if (existingOrderItem == null)
        {
            return null;
        }

        existingOrderItem.OrderId = orderItem.OrderId;
        existingOrderItem.MenuItemId = orderItem.MenuItemId;
        existingOrderItem.Quantity = orderItem.Quantity;
        existingOrderItem.UnitPrice = orderItem.UnitPrice;
        existingOrderItem.Notes = orderItem.Notes;
        await _context.SaveChangesAsync();
        return existingOrderItem;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var orderItem = await GetByIdAsync(id);
        if (orderItem == null)
        {
            return false;
        }

        _orderItems.Remove(orderItem);
        await _context.SaveChangesAsync();
        return true;
    }
}