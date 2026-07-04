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

    public async Task<Item?> UpdateAsync(int id, Item item)
    {
        var existingItem = await GetByIdAsync(id);
        if (existingItem == null)
        {
            return null;
        }

        existingItem.Name = item.Name;
        existingItem.Unit = item.Unit;
        existingItem.StockQuantity = item.StockQuantity;
        existingItem.StockThreshold = item.StockThreshold;
        existingItem.CostPerUnit = item.CostPerUnit;
        existingItem.IsActive = item.IsActive;

        await _context.SaveChangesAsync();
        return existingItem;
    }

    public async Task DeleteAsync(int id)
    {
        var item = await GetByIdAsync(id);
        if (item != null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
