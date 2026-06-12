using Microsoft.EntityFrameworkCore;
using OrderNKitchenMS_API.Data;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Repositories.Interfaces;

namespace OrderNKitchenMS_API.Repositories;

public class MenuItemRepository : IMenuItemRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<MenuItem> _menuItems;

    public MenuItemRepository(AppDbContext dbContext)
    {
        _context = dbContext;
        _menuItems = _context.MenuItems;
    }

    public Task<IQueryable<MenuItem>> GetAllAsync()
    {
        return Task.FromResult(_menuItems
            .Include(menuItem => menuItem.Category)
            .Where(menuItem => !menuItem.IsDeleted));
    }

    public async Task<MenuItem?> GetByIdAsync(int id)
    {
        return await _menuItems
            .Include(menuItem => menuItem.Category)
            .FirstOrDefaultAsync(menuItem => menuItem.Id == id && !menuItem.IsDeleted);
    }

    public async Task<MenuItem> CreateAsync(MenuItem menuItem)
    {
        _menuItems.Add(menuItem);
        await _context.SaveChangesAsync();
        return menuItem;
    }

    public async Task<MenuItem?> UpdateAsync(int id, MenuItem menuItem)
    {
        var existingMenuItem = await GetByIdAsync(id);
        if (existingMenuItem == null)
        {
            return null;
        }

        existingMenuItem.Name = menuItem.Name;
        existingMenuItem.Description = menuItem.Description;
        existingMenuItem.Price = menuItem.Price;
        existingMenuItem.CategoryId = menuItem.CategoryId;
        existingMenuItem.ImageUrl = menuItem.ImageUrl;
        existingMenuItem.PreparationTime = menuItem.PreparationTime;
        existingMenuItem.IsAvailable = menuItem.IsAvailable;
        await _context.SaveChangesAsync();
        return existingMenuItem;
    }

    //Soft-deleting
    public async Task<bool> DeleteAsync(int id)
    {
        var menuItem = await GetByIdAsync(id);
        if (menuItem == null)
        {
            return false;
        }

        menuItem.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleAvailabilityAsync(int id, bool isAvailable)
    {
        var menuItem = await GetByIdAsync(id);
        if (menuItem == null)
        {
            return false;
        }

        menuItem.IsAvailable = isAvailable;
        await _context.SaveChangesAsync();
        return true;
    }
}
