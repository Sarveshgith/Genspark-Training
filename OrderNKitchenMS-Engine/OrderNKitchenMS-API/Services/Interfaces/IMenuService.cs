using OrderNKitchenMS_API.Models.DTOs;

namespace OrderNKitchenMS_API.Services.Interfaces;

public interface IMenuService
{
    public Task<IEnumerable<MenuItemDto>> GetAllAsync(QueryMenuItemDto query);

    public Task<MenuItemDto> GetByIdAsync(int id);

    public Task<MenuItemDto> CreateAsync(MenuItemCreateDto menuItemCreateDto);

    public Task<MenuItemDto> UpdateAsync(int id, MenuItemUpdateDto menuItemUpdateDto);

    public Task<bool> ToggleAvailabilityAsync(int id, bool isAvailable);

    public Task<bool> DeleteAsync(int id);
}
