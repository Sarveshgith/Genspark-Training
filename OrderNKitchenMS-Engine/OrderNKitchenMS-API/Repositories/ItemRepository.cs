using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderNKitchenMS_API.Data;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Repositories.Interfaces;

namespace OrderNKitchenMS_API.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<Item> _items;

    public ItemRepository(AppDbContext context)
    {
        _context = context;
        _items = _context.Items;
    }

    public async Task<IEnumerable<Item>> GetAllAsync()
    {
        return await _items
            .ToListAsync();
    }

    public async Task<Item?> GetByIdAsync(int id)
    {
        return await _items
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<Item>> GetLowStockItemsAsync()
    {
        return await _items
            .Where(i => i.IsActive && i.StockQuantity <= i.StockThreshold)
            .ToListAsync();
    }

    public async Task<Item> CreateAsync(Item item)
    {
        _items.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public Task UpdateAsync(Item item)
    {
        _context.Entry(item).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id)
    {
        var item = await GetByIdAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await UpdateAsync(item);
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
