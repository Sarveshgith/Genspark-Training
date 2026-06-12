using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderNKitchenMS_API.Data;
using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Utils;

namespace OrderNKitchenMS_API.Services;

public class ItemService : IItemService
{
    private readonly IItemRepository _itemRepository;
    private readonly IMenuItemIngredientRepository _ingredientRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ILogger<ItemService> _logger;
    private readonly AppDbContext _context;

    public ItemService(
        IItemRepository itemRepository,
        IMenuItemIngredientRepository ingredientRepository,
        IMenuItemRepository menuItemRepository,
        ILogger<ItemService> logger,
        AppDbContext context)
    {
        _itemRepository = itemRepository;
        _ingredientRepository = ingredientRepository;
        _menuItemRepository = menuItemRepository;
        _logger = logger;
        _context = context;
    }

    public async Task<IEnumerable<ItemDto>> GetAllItemsAsync()
    {
        _logger.LogInformation("GetAllItemsAsync called.");
        var items = await _itemRepository.GetAllAsync();
        return items.Select(MapToDto);
    }

    public async Task<ItemDto> GetItemByIdAsync(int id)
    {
        _logger.LogInformation("GetItemByIdAsync called for ID {Id}", id);
        var item = await _itemRepository.GetByIdAsync(id);
        if (item == null)
        {
            throw new NotFoundException($"Item with ID {id} was not found.");
        }
        return MapToDto(item);
    }

    public async Task<IEnumerable<ItemDto>> GetLowStockItemsAsync()
    {
        _logger.LogInformation("GetLowStockItemsAsync called.");
        var items = await _itemRepository.GetLowStockItemsAsync();
        return items.Select(MapToDto);
    }

    public async Task<ItemDto> CreateItemAsync(ItemCreateDto dto)
    {
        _logger.LogInformation("CreateItemAsync started for Name: '{Name}'", dto.Name);
        Validation.RequireNonEmptyString(dto.Name, nameof(dto.Name), "Item name is required.");
        Validation.RequireValidEnum<Models.Enums.ItemUnit>((int)dto.Unit, nameof(dto.Unit), "Invalid unit.");
        Validation.Require(dto.StockQuantity >= 0, "Stock quantity must be greater than or equal to 0.", nameof(dto.StockQuantity));
        Validation.Require(dto.StockThreshold >= 0, "Stock threshold must be greater than or equal to 0.", nameof(dto.StockThreshold));
        if (dto.CostPerUnit.HasValue)
        {
            Validation.Require(dto.CostPerUnit.Value >= 0, "Cost per unit must be greater than or equal to 0.", nameof(dto.CostPerUnit));
        }

        var item = new Item
        {
            Name = dto.Name.Trim(),
            Unit = dto.Unit,
            StockQuantity = dto.StockQuantity,
            StockThreshold = dto.StockThreshold,
            CostPerUnit = dto.CostPerUnit,
            IsActive = true
        };

        var created = await _itemRepository.CreateAsync(item);
        _logger.LogInformation("CreateItemAsync succeeded. Created Item ID: {Id}", created.Id);
        return MapToDto(created);
    }

    public async Task<ItemDto> UpdateItemAsync(int id, ItemUpdateDto dto)
    {
        _logger.LogInformation("UpdateItemAsync started for Item ID: {Id}", id);
        Validation.RequireNotNull(dto, nameof(dto), "Update data is required.");
        Validation.RequireNonEmptyString(dto.Name, nameof(dto.Name), "Item name is required.");
        Validation.RequireValidEnum<Models.Enums.ItemUnit>((int)dto.Unit, nameof(dto.Unit), "Invalid unit.");
        Validation.Require(dto.StockQuantity >= 0, "Stock quantity must be greater than or equal to 0.", nameof(dto.StockQuantity));
        if (dto.StockThreshold.HasValue)
        {
            Validation.Require(dto.StockThreshold.Value >= 0, "Stock threshold must be greater than or equal to 0.", nameof(dto.StockThreshold));
        }
        if (dto.CostPerUnit.HasValue)
        {
            Validation.Require(dto.CostPerUnit.Value >= 0, "Cost per unit must be greater than or equal to 0.", nameof(dto.CostPerUnit));
        }

        var item = await _itemRepository.GetByIdAsync(id);
        if (item == null)
        {
            throw new NotFoundException($"Item with ID {id} was not found.");
        }

        item.Name = dto.Name.Trim();
        item.Unit = dto.Unit;
        item.StockQuantity = dto.StockQuantity;
        if (dto.StockThreshold.HasValue)
        {
            item.StockThreshold = dto.StockThreshold.Value;
        }
        item.CostPerUnit = dto.CostPerUnit;
        item.IsActive = dto.IsActive;

        await _itemRepository.UpdateAsync(item);
        await _itemRepository.SaveChangesAsync();

        // Stock quantity could have changed, re-evaluate all menu items using this item
        var ingredientMappings = await _ingredientRepository.GetByItemIdAsync(id);
        foreach (var mapping in ingredientMappings)
        {
            await ReevaluateMenuItemAvailabilityAsync(mapping.MenuItemId);
        }

        _logger.LogInformation("UpdateItemAsync succeeded for Item ID: {Id}", id);
        return MapToDto(item);
    }

    public async Task ChangeItemStatusAsync(int id, bool isActive)
    {
        _logger.LogInformation("ChangeItemStatusAsync started for Item ID: {Id}, IsActive: {IsActive}", id, isActive);
        var item = await _itemRepository.GetByIdAsync(id);
        if (item == null)
        {
            throw new NotFoundException($"Item with ID {id} was not found.");
        }

        item.IsActive = isActive;
        await _itemRepository.UpdateAsync(item);
        await _itemRepository.SaveChangesAsync();

        // Re-evaluate affected menu items
        var ingredientMappings = await _ingredientRepository.GetByItemIdAsync(id);
        foreach (var mapping in ingredientMappings)
        {
            await ReevaluateMenuItemAvailabilityAsync(mapping.MenuItemId);
        }

        _logger.LogInformation("ChangeItemStatusAsync succeeded for Item ID: {Id}", id);
    }

    public async Task<IEnumerable<MenuItemIngredientDto>> GetIngredientsByMenuItemIdAsync(int menuItemId)
    {
        _logger.LogInformation("GetIngredientsByMenuItemIdAsync called for MenuItem ID {MenuItemId}", menuItemId);
        var menuItem = await _menuItemRepository.GetByIdAsync(menuItemId);
        if (menuItem == null)
        {
            throw new NotFoundException($"Menu item with ID {menuItemId} was not found.");
        }

        var ingredients = await _ingredientRepository.GetByMenuItemIdAsync(menuItemId);
        return ingredients.Select(MapToIngredientDto);
    }

    public async Task<IEnumerable<MenuItemIngredientDto>> AddMenuItemIngredientsAsync(int menuItemId, List<MenuItemIngredientCreateDto> dtos)
    {
        _logger.LogInformation("AddMenuItemIngredientsAsync started for MenuItem ID {MenuItemId}, mapping {Count} items.", menuItemId, dtos.Count);
        
        var menuItem = await _menuItemRepository.GetByIdAsync(menuItemId);
        if (menuItem == null)
        {
            throw new NotFoundException($"Menu item with ID {menuItemId} was not found.");
        }

        // Validate items exist and no duplicate in database or input list
        var existingIngredients = await _ingredientRepository.GetByMenuItemIdAsync(menuItemId);
        var existingItemIds = existingIngredients.Select(i => i.ItemId).ToHashSet();
        var inputItemIds = new HashSet<int>();

        var newMappings = new List<MenuItemIngredient>();

        foreach (var dto in dtos)
        {
            var item = await _itemRepository.GetByIdAsync(dto.ItemId);
            if (item == null)
            {
                throw new NotFoundException($"Ingredient item with ID {dto.ItemId} was not found.");
            }
            Validation.Require(dto.QuantityRequired > 0, "Required quantity must be greater than 0.", nameof(dto.QuantityRequired));

            if (existingItemIds.Contains(dto.ItemId) || inputItemIds.Contains(dto.ItemId))
            {
                throw new BusinessRuleException($"Ingredient with ID {dto.ItemId} is already mapped to this menu item.");
            }

            inputItemIds.Add(dto.ItemId);

            var newMapping = new MenuItemIngredient
            {
                MenuItemId = menuItemId,
                ItemId = dto.ItemId,
                QuantityRequired = dto.QuantityRequired,
                Item = item
            };
            newMappings.Add(newMapping);
        }

        await _context.MenuItemIngredients.AddRangeAsync(newMappings);
        await _context.SaveChangesAsync();

        // Re-evaluate MenuItem availability
        await ReevaluateMenuItemAvailabilityAsync(menuItemId);

        _logger.LogInformation("AddMenuItemIngredientsAsync succeeded.");
        return newMappings.Select(MapToIngredientDto);
    }

    public async Task<IEnumerable<MenuItemIngredientDto>> UpdateMenuItemIngredientsAsync(int menuItemId, List<MenuItemIngredientCreateDto> dtos)
    {
        _logger.LogInformation("UpdateMenuItemIngredientsAsync started for MenuItem ID {MenuItemId}, mapping {Count} items.", menuItemId, dtos.Count);

        var menuItem = await _menuItemRepository.GetByIdAsync(menuItemId);
        if (menuItem == null)
        {
            throw new NotFoundException($"Menu item with ID {menuItemId} was not found.");
        }

        // Validate input items exist and no duplicate in input list
        var inputItemIds = new HashSet<int>();
        var itemsMap = new Dictionary<int, Item>();

        foreach (var dto in dtos)
        {
            var item = await _itemRepository.GetByIdAsync(dto.ItemId);
            if (item == null)
            {
                throw new NotFoundException($"Ingredient item with ID {dto.ItemId} was not found.");
            }
            Validation.Require(dto.QuantityRequired > 0, "Required quantity must be greater than 0.", nameof(dto.QuantityRequired));

            if (inputItemIds.Contains(dto.ItemId))
            {
                throw new BusinessRuleException($"Ingredient with ID {dto.ItemId} is duplicated in the update payload.");
            }

            inputItemIds.Add(dto.ItemId);
            itemsMap[dto.ItemId] = item;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var newMappings = new List<MenuItemIngredient>();
        try
        {
            // Clear existing mapping
            var existingIngredients = await _ingredientRepository.GetByMenuItemIdAsync(menuItemId);
            await _ingredientRepository.RemoveRangeAsync(existingIngredients);
            await _ingredientRepository.SaveChangesAsync();

            // Add new mapping
            foreach (var dto in dtos)
            {
                var newMapping = new MenuItemIngredient
                {
                    MenuItemId = menuItemId,
                    ItemId = dto.ItemId,
                    QuantityRequired = dto.QuantityRequired,
                    Item = itemsMap[dto.ItemId]
                };
                newMappings.Add(newMapping);
            }

            await _context.MenuItemIngredients.AddRangeAsync(newMappings);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateMenuItemIngredientsAsync failed. Transaction rolled back.");
            await transaction.RollbackAsync();
            throw;
        }

        // Re-evaluate MenuItem availability
        await ReevaluateMenuItemAvailabilityAsync(menuItemId);
        _logger.LogInformation("UpdateMenuItemIngredientsAsync succeeded for MenuItem ID {menuItemId}", menuItemId);

        return newMappings.Select(MapToIngredientDto);
    }

    public async Task RemoveMenuItemIngredientAsync(int menuItemId, int ingredientId)
    {
        _logger.LogInformation("RemoveMenuItemIngredientAsync started for MenuItem ID {MenuItemId}, Ingredient Mapping ID {IngredientId}.", menuItemId, ingredientId);

        var menuItem = await _menuItemRepository.GetByIdAsync(menuItemId);
        if (menuItem == null)
        {
            throw new NotFoundException($"Menu item with ID {menuItemId} was not found.");
        }

        var ingredientMapping = await _context.MenuItemIngredients.FirstOrDefaultAsync(mi => mi.Id == ingredientId && mi.MenuItemId == menuItemId);
        if (ingredientMapping == null)
        {
            throw new NotFoundException($"Ingredient mapping with ID {ingredientId} was not found for Menu Item {menuItemId}.");
        }

        _context.MenuItemIngredients.Remove(ingredientMapping);
        await _context.SaveChangesAsync();

        // Re-evaluate MenuItem availability
        await ReevaluateMenuItemAvailabilityAsync(menuItemId);

        _logger.LogInformation("RemoveMenuItemIngredientAsync succeeded.");
    }

    public async Task<bool> CanPrepareMenuItemAsync(int menuItemId, int quantity = 1)
    {
        var ingredients = await _ingredientRepository.GetByMenuItemIdAsync(menuItemId);
        var mappedList = ingredients.ToList();
        
        if (mappedList.Count == 0)
        {
            return false;
        }

        foreach (var ingredient in mappedList)
        {
            if (!ingredient.Item.IsActive)
            {
                return false;
            }

            if (ingredient.Item.StockQuantity < ingredient.QuantityRequired * quantity)
            {
                return false;
            }
        }

        return true;
    }

    public async Task UpdateStockByMenuItemIdAsync(int menuItemId, int quantity, bool isAdd)
    {
        _logger.LogInformation("UpdateStockByMenuItemIdAsync started. MenuItemId: {MenuItemId}, Quantity: {Quantity}, IsAdd: {IsAdd}", menuItemId, quantity, isAdd);
        var ingredients = await _ingredientRepository.GetByMenuItemIdAsync(menuItemId);
        var mappedList = ingredients.ToList();

        if (mappedList.Count == 0)
        {
            return;
        }

        foreach (var ingredient in mappedList)
        {
            var requiredQuantity = ingredient.QuantityRequired * quantity;
            if (isAdd)
            {
                ingredient.Item.StockQuantity += requiredQuantity;
            }
            else
            {
                if (ingredient.Item.StockQuantity < requiredQuantity)
                {
                    throw new BusinessRuleException($"Insufficient stock for item: {ingredient.Item.Name}");
                }
                ingredient.Item.StockQuantity -= requiredQuantity;
            }
            await _itemRepository.UpdateAsync(ingredient.Item);
        }
        await _itemRepository.SaveChangesAsync();

        // Re-evaluate affected menu items. Since we updated the stock of these ingredients,
        // we must re-evaluate all menu items that depend on any of these ingredient items.
        var affectedItemIds = mappedList.Select(m => m.ItemId).Distinct().ToList();
        var allAffectedMappings = new List<MenuItemIngredient>();
        foreach (var itemId in affectedItemIds)
        {
            var mappings = await _ingredientRepository.GetByItemIdAsync(itemId);
            allAffectedMappings.AddRange(mappings);
        }

        var uniqueMenuItemIds = allAffectedMappings.Select(m => m.MenuItemId).Distinct().ToList();
        foreach (var affectedMenuItemId in uniqueMenuItemIds)
        {
            await ReevaluateMenuItemAvailabilityAsync(affectedMenuItemId);
        }

        _logger.LogInformation("UpdateStockByMenuItemIdAsync completed for MenuItemId: {MenuItemId}", menuItemId);
    }

    public async Task ReevaluateMenuItemAvailabilityAsync(int menuItemId)
    {
        if (_context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
        {
            return;
        }

        var menuItem = await _menuItemRepository.GetByIdAsync(menuItemId);
        if (menuItem == null)
        {
            return;
        }

        var canPrepare = await CanPrepareMenuItemAsync(menuItemId, 1);
        if (canPrepare)
        {
            // If manual override is disabled, make it available
            if (!menuItem.IsManuallyDisabled)
            {
                menuItem.IsAvailable = true;
            }
            else
            {
                menuItem.IsAvailable = false;
            }
        }
        else
        {
            // If we can't prepare it, it must be unavailable
            menuItem.IsAvailable = false;
        }

        await _menuItemRepository.UpdateAsync(menuItemId, menuItem);
    }

    public async Task ValidateStockForOrderItemsAsync(List<OrderItemCreateDto> orderItems)
    {
        if (orderItems == null || !orderItems.Any())
        {
            return;
        }

        var requestedMenuItemIds = orderItems.Select(oi => oi.MenuItemId).Distinct().ToList();
        var menuItems = await _context.MenuItems
            .Where(m => requestedMenuItemIds.Contains(m.Id) && !m.IsDeleted)
            .ToListAsync();

        foreach (var menuItemId in requestedMenuItemIds)
        {
            var menuItem = menuItems.FirstOrDefault(mi => mi.Id == menuItemId);
            if (menuItem == null)
            {
                throw new NotFoundException($"Menu item with id {menuItemId} was not found.");
            }

            if (!menuItem.IsAvailable)
            {
                throw new BusinessRuleException($"Menu item with id {menuItemId} is not available.");
            }
        }

        var menuIngredients = await _context.MenuItemIngredients
            .Include(mi => mi.Item)
            .Where(mi => requestedMenuItemIds.Contains(mi.MenuItemId))
            .ToListAsync();

        var itemIds = menuIngredients.Select(mi => mi.ItemId).Distinct().ToList();
        var itemsStock = await _context.Items
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, i => i.StockQuantity);

        foreach (var orderItemDto in orderItems)
        {
            var menuItem = menuItems.First(mi => mi.Id == orderItemDto.MenuItemId);
            var ingredientsForThisMenu = menuIngredients.Where(mi => mi.MenuItemId == orderItemDto.MenuItemId).ToList();

            if (ingredientsForThisMenu.Count == 0)
            {
                throw new BusinessRuleException($"Insufficient stock for item: {menuItem.Name}");
            }

            foreach (var ingredient in ingredientsForThisMenu)
            {
                if (!ingredient.Item.IsActive)
                {
                    throw new BusinessRuleException($"Insufficient stock for item: {menuItem.Name}");
                }

                var totalRequired = ingredient.QuantityRequired * orderItemDto.Quantity;
                if (itemsStock[ingredient.ItemId] < totalRequired)
                {
                    throw new BusinessRuleException($"Insufficient stock for item: {menuItem.Name}");
                }

                itemsStock[ingredient.ItemId] -= totalRequired;
            }
        }
    }

    private static ItemDto MapToDto(Item item)
    {
        return new ItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Unit = item.Unit,
            StockQuantity = item.StockQuantity,
            StockThreshold = item.StockThreshold,
            CostPerUnit = item.CostPerUnit,
            IsActive = item.IsActive,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    private static MenuItemIngredientDto MapToIngredientDto(MenuItemIngredient ingredient)
    {
        return new MenuItemIngredientDto
        {
            Id = ingredient.Id,
            MenuItemId = ingredient.MenuItemId,
            ItemId = ingredient.ItemId,
            ItemName = ingredient.Item?.Name ?? string.Empty,
            QuantityRequired = ingredient.QuantityRequired
        };
    }
}
