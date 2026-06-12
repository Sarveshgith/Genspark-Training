using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderNKitchenMS_API.Data;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Repositories.Interfaces;

namespace OrderNKitchenMS_API.Repositories;

public class MenuItemIngredientRepository : IMenuItemIngredientRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<MenuItemIngredient> _ingredients;

    public MenuItemIngredientRepository(AppDbContext context)
    {
        _context = context;
        _ingredients = _context.MenuItemIngredients;
    }

    public async Task<IEnumerable<MenuItemIngredient>> GetByMenuItemIdAsync(int menuItemId)
    {
        return await _ingredients
            .Include(mi => mi.Item)
            .Where(mi => mi.MenuItemId == menuItemId)
            .ToListAsync();
    }

    public async Task<IEnumerable<MenuItemIngredient>> GetByItemIdAsync(int itemId)
    {
        return await _ingredients
            .Include(mi => mi.MenuItem)
            .Where(mi => mi.ItemId == itemId)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<MenuItemIngredient> ingredients)
    {
        await _ingredients.AddRangeAsync(ingredients);
    }

    public Task RemoveRangeAsync(IEnumerable<MenuItemIngredient> ingredients)
    {
        _ingredients.RemoveRange(ingredients);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
