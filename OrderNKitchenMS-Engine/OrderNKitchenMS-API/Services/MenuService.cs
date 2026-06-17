using Microsoft.EntityFrameworkCore;
using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Utils;
using Microsoft.Extensions.Logging;

namespace OrderNKitchenMS_API.Services;

public class MenuService : IMenuService
{
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IItemService _itemService;
    private readonly ILogger<MenuService> _logger;

    public MenuService(
        IMenuItemRepository menuItemRepository, 
        ICategoryRepository categoryRepository, 
        IItemService itemService,
        ILogger<MenuService> logger)
    {
        _menuItemRepository = menuItemRepository;
        _categoryRepository = categoryRepository;
        _itemService = itemService;
        _logger = logger;
    }

    // Retrieves a filtered, paginated list of menu items.
    public async Task<IEnumerable<MenuItemDto>> GetAllAsync(QueryMenuItemDto query)
    {
        _logger.LogInformation("GetAllAsync called for menu items. CategoryId: {CategoryId}, Search: '{Name}'", query?.CategoryId, query?.Name);
        var menuItems = await _menuItemRepository.GetAllAsync();

        if (Validation.IsNonEmptyString(query.Name ?? string.Empty))
        {
            menuItems = menuItems.Where(menuItem => menuItem.Name.Contains(query.Name!));
        }

        if (query.CategoryId.HasValue)
        {
            menuItems = menuItems.Where(menuItem => menuItem.CategoryId == query.CategoryId.Value);
        }

        if (query.MinPrice.HasValue)
        {
            menuItems = menuItems.Where(menuItem => menuItem.Price >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            menuItems = menuItems.Where(menuItem => menuItem.Price <= query.MaxPrice.Value);
        }

        if (query.IsAvailable.HasValue)
        {
            menuItems = menuItems.Where(menuItem => menuItem.IsAvailable == query.IsAvailable.Value);
        }

        if (query.MaxPreparationTime.HasValue)
        {
            menuItems = menuItems.Where(menuItem => menuItem.PreparationTime <= query.MaxPreparationTime.Value);
        }

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        var result = await menuItems
            .OrderBy(menuItem => menuItem.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        _logger.LogInformation("GetAllAsync completed. Returned {Count} menu items.", result.Count);
        return result.Select(MapMenuItemToDto);
    }

    // Retrieves a specific menu item by its unique identifier.
    public async Task<MenuItemDto> GetByIdAsync(int id)
    {
        _logger.LogInformation("GetByIdAsync called for Menu Item ID: {Id}", id);
        var menuItem = await _menuItemRepository.GetByIdAsync(id);
        if (menuItem == null)
        {
            _logger.LogWarning("GetByIdAsync failed: Menu Item with ID {Id} was not found.", id);
            throw new NotFoundException($"Menu item with id {id} was not found.");
        }

        return MapMenuItemToDto(menuItem);
    }

    // Creates a new menu item.
    public async Task<MenuItemDto> CreateAsync(MenuItemCreateDto menuItemCreateDto)
    {
        _logger.LogInformation("CreateAsync started for Menu Item Name: '{Name}'", menuItemCreateDto.Name);
        ValidateCreateDto(menuItemCreateDto);
        await EnsureCategoryExists(menuItemCreateDto.CategoryId);
        await EnsureUniqueNameAsync(menuItemCreateDto.Name);

        var menuItemEntity = MapCreateDtoToEntity(menuItemCreateDto);
        menuItemEntity.IsAvailable = false;
        menuItemEntity.IsManuallyDisabled = false;

        var createdMenuItem = await _menuItemRepository.CreateAsync(menuItemEntity);
        _logger.LogInformation("CreateAsync succeeded. Created Menu Item ID: {Id}", createdMenuItem.Id);
        return MapMenuItemToDto(createdMenuItem);
    }

    // Updates the details of an existing menu item.
    public async Task<MenuItemDto> UpdateAsync(int id, MenuItemUpdateDto menuItemUpdateDto)
    {
        _logger.LogInformation("UpdateAsync started for Menu Item ID: {Id}", id);
        ValidateUpdateDto(menuItemUpdateDto);
        await EnsureCategoryExists(menuItemUpdateDto.CategoryId);
        await EnsureUniqueNameAsync(menuItemUpdateDto.Name, id);

        var menuItemEntity = MapUpdateDtoToEntity(menuItemUpdateDto);
        var updatedMenuItem = await _menuItemRepository.UpdateAsync(id, menuItemEntity);
        if (updatedMenuItem == null)
        {
            _logger.LogWarning("UpdateAsync failed: Menu Item with ID {Id} was not found.", id);
            throw new NotFoundException($"Menu item with id {id} was not found.");
        }

        _logger.LogInformation("UpdateAsync succeeded for Menu Item ID: {Id}", id);
        return MapMenuItemToDto(updatedMenuItem);
    }

    private async Task EnsureUniqueNameAsync(string name, int? excludeId = null)
    {
        var menuItems = await _menuItemRepository.GetAllAsync();
        var exists = await menuItems.AnyAsync(m => m.Name == name && (!excludeId.HasValue || m.Id != excludeId.Value));
        if (exists)
        {
            throw new ConflictException($"Menu item with name '{name}' already exists.");
        }
    }

    // Toggles the availability status of a menu item.
    public async Task<bool> ToggleAvailabilityAsync(int id, bool isAvailable)
    {
        _logger.LogInformation("ToggleAvailabilityAsync started for Menu Item ID: {Id}, Target Availability: {IsAvailable}", id, isAvailable);
        var menuItem = await _menuItemRepository.GetByIdAsync(id);
        if (menuItem == null)
        {
            _logger.LogWarning("ToggleAvailabilityAsync failed: Menu Item with ID {Id} was not found.", id);
            throw new NotFoundException($"Menu item with id {id} was not found.");
        }

        if (isAvailable)
        {
            var canPrepare = await _itemService.CanPrepareMenuItemAsync(id, 1);
            if (!canPrepare)
            {
                throw new BusinessRuleException("Cannot enable item with zero stock");
            }
            menuItem.IsAvailable = true;
            menuItem.IsManuallyDisabled = false;
        }
        else
        {
            menuItem.IsAvailable = false;
            menuItem.IsManuallyDisabled = true;
        }

        await _menuItemRepository.UpdateAsync(id, menuItem);
        _logger.LogInformation("ToggleAvailabilityAsync succeeded for Menu Item ID: {Id}", id);
        return true;
    }

    // Marks a specific menu item as deleted.
    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("DeleteAsync started for Menu Item ID: {Id}", id);
        var menuItem = await _menuItemRepository.GetByIdAsync(id);
        if (menuItem == null)
        {
            _logger.LogWarning("DeleteAsync failed: Menu Item with ID {Id} was not found.", id);
            throw new NotFoundException($"Menu item with id {id} was not found.");
        }

        menuItem.IsAvailable = false;
        menuItem.IsManuallyDisabled = true;
        menuItem.IsDeleted = true;
        await _menuItemRepository.UpdateAsync(id, menuItem);

        _logger.LogInformation("DeleteAsync succeeded for Menu Item ID: {Id}", id);
        return true;
    }


    // Validates that a category exists.
    private async Task EnsureCategoryExists(int categoryId)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId);
        if (category == null)
        {
            _logger.LogWarning("Category existence check failed: Category with ID {Id} was not found.", categoryId);
            throw new NotFoundException($"Category with id {categoryId} was not found.");
        }
    }

    private static void ValidateCreateDto(MenuItemCreateDto menuItemCreateDto)
    {
        Validation.RequireNotNull(menuItemCreateDto, nameof(menuItemCreateDto), "Menu item data is required.");
        ValidateName(menuItemCreateDto.Name);
    }

    private static void ValidateUpdateDto(MenuItemUpdateDto menuItemUpdateDto)
    {
        Validation.RequireNotNull(menuItemUpdateDto, nameof(menuItemUpdateDto), "Menu item data is required.");
        ValidateName(menuItemUpdateDto.Name);
    }

    private static void ValidateName(string name)
    {
        Validation.RequireNonEmptyString(name, nameof(name), "Name is required.");
    }

    private static MenuItemDto MapMenuItemToDto(MenuItem menuItem)
    {
        return new MenuItemDto
        {
            Id = menuItem.Id,
            Name = menuItem.Name,
            Description = menuItem.Description,
            Price = menuItem.Price,
            CategoryId = menuItem.CategoryId,
            CategoryName = menuItem.Category?.Name ?? string.Empty,
            ImageUrl = menuItem.ImageUrl,
            PreparationTime = menuItem.PreparationTime,
            IsAvailable = menuItem.IsAvailable,
            CreatedAt = menuItem.CreatedAt
        };
    }

    private static MenuItem MapCreateDtoToEntity(MenuItemCreateDto menuItemCreateDto)
    {
        return new MenuItem
        {
            Name = menuItemCreateDto.Name,
            Description = menuItemCreateDto.Description,
            Price = menuItemCreateDto.Price,
            CategoryId = menuItemCreateDto.CategoryId,
            ImageUrl = menuItemCreateDto.ImageUrl,
            PreparationTime = menuItemCreateDto.PreparationTime,
            IsAvailable = menuItemCreateDto.IsAvailable
        };
    }

    private static MenuItem MapUpdateDtoToEntity(MenuItemUpdateDto menuItemUpdateDto)
    {
        return new MenuItem
        {
            Name = menuItemUpdateDto.Name,
            Description = menuItemUpdateDto.Description,
            Price = menuItemUpdateDto.Price,
            CategoryId = menuItemUpdateDto.CategoryId,
            ImageUrl = menuItemUpdateDto.ImageUrl,
            PreparationTime = menuItemUpdateDto.PreparationTime,
            IsAvailable = menuItemUpdateDto.IsAvailable
        };
    }
}
