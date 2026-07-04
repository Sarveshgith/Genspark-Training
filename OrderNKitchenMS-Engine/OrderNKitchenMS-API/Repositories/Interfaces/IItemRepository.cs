using System.Collections.Generic;
using System.Threading.Tasks;
using OrderNKitchenMS_API.Models.Entities;

namespace OrderNKitchenMS_API.Repositories.Interfaces;

public interface IItemRepository
{
    Task<IEnumerable<Item>> GetAllAsync();
    Task<Item?> GetByIdAsync(int id);
    Task<IEnumerable<Item>> GetLowStockItemsAsync();
    Task<Item> CreateAsync(Item item);
    Task<Item?> UpdateAsync(int id, Item item);
    Task DeleteAsync(int id);
    Task SaveChangesAsync();
}
