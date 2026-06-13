using Microsoft.EntityFrameworkCore;
using OrderNKitchenMS_API.Data;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Models.Enums;
using OrderNKitchenMS_API.Repositories.Interfaces;

namespace OrderNKitchenMS_API.Repositories;

public class OrderRepository : IOrderRepository
{
	private readonly AppDbContext _context;
	private readonly DbSet<Order> _orders;
    private readonly IOrderItemRepository _orderItemRepository;

	public OrderRepository(AppDbContext context, IOrderItemRepository orderItemRepository)
	{
		_context = context;
		_orders = _context.Orders;
		_orderItemRepository = orderItemRepository;
	}

	public async Task<IEnumerable<Order>> GetAllAsync()
	{
		return await _orders
			.Include(order => order.Table)
			.Include(order => order.AssignedChef)
			.Include(order => order.AssignedWaiter)
			.ToListAsync();
	}

	public async Task<Order?> GetByIdAsync(int id)
	{
		return await _orders
			.Include(order => order.Table)
			.Include(order => order.AssignedChef)
			.Include(order => order.AssignedWaiter)
			.FirstOrDefaultAsync(order => order.Id == id);
	}

	public async Task<Order> AddAsync(Order order)
	{
		_orders.Add(order);
		return order;
	}

	public async Task<Order?> UpdateAsync(int id, Order order)
	{
		var existingOrder = await GetByIdAsync(id);
		if (existingOrder == null)
		{
			return null;
		}

		existingOrder.TableId = order.TableId;
		existingOrder.AssignedChefId = order.AssignedChefId;
		existingOrder.AssignedWaiterId = order.AssignedWaiterId;
		existingOrder.Status = order.Status;
		existingOrder.TotalAmount = order.TotalAmount;
		existingOrder.CompletedAt = order.CompletedAt;
		await _context.SaveChangesAsync();
		return existingOrder;
	}

	public async Task<Order?> AddMenuItemAsync(int orderId, OrderItem orderItem)
	{
		var order = await GetByIdAsync(orderId);
		if (order == null)
		{
			return null;
		}

		orderItem.OrderId = orderId;
		await _orderItemRepository.AddAsync(orderItem);
		await _context.SaveChangesAsync();
		return order;
	}

	public async Task<bool> UpdateStatusAsync(int id, OrderStatus status)
	{
		var order = await GetByIdAsync(id);
		if (order == null)
		{
			return false;
		}

		order.Status = status;
		if (status == OrderStatus.Completed)
		{
			order.CompletedAt = DateTime.UtcNow;
		}

		if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
		{
			if (status == OrderStatus.Completed || status == OrderStatus.Cancelled)
			{
				if (order.Table != null)
				{
					order.Table.Status = TableStatus.Available;
				}
			}
		}

		await _context.SaveChangesAsync();
		return true;
	}

	public async Task<bool> CancelAsync(int id)
	{
		var order = await GetByIdAsync(id);
		if (order == null)
		{
			return false;
		}

		order.Status = OrderStatus.Cancelled;
		await _context.SaveChangesAsync();
		return true;
	}

}
