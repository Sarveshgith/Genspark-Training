using System.Collections.Generic;
using System.Threading.Tasks;
using OrderNKitchenMS_API.Models.Entities;

namespace OrderNKitchenMS_API.Repositories.Interfaces;

public interface IMenuItemIngredientRepository
{
    Task<IEnumerable<MenuItemIngredient>> GetByMenuItemIdAsync(int menuItemId);
    Task<IEnumerable<MenuItemIngredient>> GetByItemIdAsync(int itemId);
    Task AddRangeAsync(IEnumerable<MenuItemIngredient> ingredients);
    Task RemoveRangeAsync(IEnumerable<MenuItemIngredient> ingredients);
    Task SaveChangesAsync();
}
