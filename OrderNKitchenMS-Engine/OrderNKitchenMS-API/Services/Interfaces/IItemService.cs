using System.Collections.Generic;
using System.Threading.Tasks;
using OrderNKitchenMS_API.Models.DTOs;

namespace OrderNKitchenMS_API.Services.Interfaces;

public interface IItemService
{
    Task<IEnumerable<ItemDto>> GetAllItemsAsync();
    Task<ItemDto> GetItemByIdAsync(int id);
    Task<IEnumerable<ItemDto>> GetLowStockItemsAsync();
    Task<ItemDto> CreateItemAsync(ItemCreateDto dto);
    Task<ItemDto> UpdateItemAsync(int id, ItemUpdateDto dto);
    Task ChangeItemStatusAsync(int id, bool isActive);

    Task<IEnumerable<MenuItemIngredientDto>> GetIngredientsByMenuItemIdAsync(int menuItemId);
    Task<IEnumerable<MenuItemIngredientDto>> AddMenuItemIngredientsAsync(int menuItemId, List<MenuItemIngredientCreateDto> dtos);
    Task<IEnumerable<MenuItemIngredientDto>> UpdateMenuItemIngredientsAsync(int menuItemId, List<MenuItemIngredientCreateDto> dtos);
    Task RemoveMenuItemIngredientAsync(int menuItemId, int ingredientId);

    Task<bool> CanPrepareMenuItemAsync(int menuItemId, int quantity = 1);
    Task UpdateStockByMenuItemIdAsync(int menuItemId, int quantity, bool isAdd);
    Task ReevaluateMenuItemAvailabilityAsync(int menuItemId);
    Task ValidateStockForOrderItemsAsync(List<OrderItemCreateDto> orderItems);
}
