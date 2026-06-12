using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Models.Entities;

namespace OrderNKitchenMS_API.Repositories.Interfaces;

public interface IMenuItemRepository
{
    public Task<IQueryable<MenuItem>> GetAllAsync();

    public Task<MenuItem?> GetByIdAsync(int id);

    public Task<MenuItem> CreateAsync(MenuItem menuItem);

    public Task<MenuItem?> UpdateAsync(int id, MenuItem menuItem);

    public Task<bool> DeleteAsync(int id);

    public Task<bool> ToggleAvailabilityAsync(int id, bool isAvailable);
}
