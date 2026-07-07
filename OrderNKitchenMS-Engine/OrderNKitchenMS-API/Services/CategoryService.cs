using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace OrderNKitchenMS_API.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMenuItemRepository _menuItemRepository;
    private readonly ILogger<CategoryService> _logger;
    private readonly IMemoryCache _cache;

    public CategoryService(
        ICategoryRepository categoryRepository, 
        IMenuItemRepository menuItemRepository,
        ILogger<CategoryService> logger,
        IMemoryCache cache)
    {
        _categoryRepository = categoryRepository;
        _menuItemRepository = menuItemRepository;
        _logger = logger;
        _cache = cache;
    }

    // Retrieves all categories, optionally filtered by food type (veg/non-veg).
    public async Task<IEnumerable<CategoryDto>> GetAllAsync(bool? isNonVeg = null)
    {
        _logger.LogInformation("GetAllAsync called with isNonVeg: {IsNonVeg}", isNonVeg);
        
        List<Category> allCategories;
        if (!_cache.TryGetValue(CacheKeys.CategoriesAll, out allCategories))
        {
            var categoriesEnumerable = await _categoryRepository.GetAllAsync(null);
            allCategories = categoriesEnumerable.ToList();
            _cache.Set(CacheKeys.CategoriesAll, allCategories, TimeSpan.FromSeconds(60));
        }

        var result = allCategories.AsEnumerable();
        if (isNonVeg.HasValue)
        {
            result = result.Where(category => category.IsNonVeg == isNonVeg.Value);
        }

        _logger.LogInformation("GetAllAsync completed. Returned {Count} categories.", result.Count());
        return result.Select(MapCategoryToDto);
    }

    // Retrieves a specific category by its unique identifier.
    public async Task<CategoryDto> GetByIdAsync(int id)
    {
        _logger.LogInformation("GetByIdAsync called for Category ID: {Id}", id);
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null)
        {
            _logger.LogWarning("GetByIdAsync failed: Category with ID {Id} was not found.", id);
            throw new NotFoundException($"Category with id {id} was not found.");
        }

        return MapCategoryToDto(category);
    }

    // Creates a new menu category.
    public async Task<CategoryDto> CreateAsync(CategoryCreateDto categoryCreateDto)
    {
        _logger.LogInformation("CreateAsync started for Category Name: '{Name}'", categoryCreateDto.Name);
        Validation.RequireNotNull(categoryCreateDto, nameof(categoryCreateDto), "Category data is required.");
        ValidateName(categoryCreateDto.Name);

        await EnsureUniqueNameAsync(categoryCreateDto.Name, categoryCreateDto.IsNonVeg);

        var createdCategory = await _categoryRepository.CreateAsync(new Category
        {
            Name = categoryCreateDto.Name.Trim(),
            IsNonVeg = categoryCreateDto.IsNonVeg
        });

        _cache.Remove(CacheKeys.CategoriesAll);
        _logger.LogInformation("CreateAsync succeeded. Created Category ID: {Id}", createdCategory.Id);
        return MapCategoryToDto(createdCategory);
    }

    // Updates the details of an existing category.
    public async Task<CategoryDto> UpdateAsync(int id, CategoryUpdateDto categoryUpdateDto)
    {
        _logger.LogInformation("UpdateAsync started for Category ID: {Id}", id);
        Validation.RequireNotNull(categoryUpdateDto, nameof(categoryUpdateDto), "Category data is required.");
        ValidateName(categoryUpdateDto.Name);

        await EnsureUniqueNameAsync(categoryUpdateDto.Name, categoryUpdateDto.IsNonVeg, id);

        var updatedCategory = await _categoryRepository.UpdateAsync(id, new Category
        {
            Name = categoryUpdateDto.Name.Trim(),
            IsNonVeg = categoryUpdateDto.IsNonVeg
        });

        if (updatedCategory == null)
        {
            _logger.LogWarning("UpdateAsync failed: Category with ID {Id} was not found.", id);
            throw new NotFoundException($"Category with id {id} was not found.");
        }

        _cache.Remove(CacheKeys.CategoriesAll);
        _cache.Remove(CacheKeys.MenuAll);
        _logger.LogInformation("UpdateAsync succeeded for Category ID: {Id}", id);
        return MapCategoryToDto(updatedCategory);
    }

    private async Task EnsureUniqueNameAsync(string name, bool isNonVeg, int? excludeId = null)
    {
        var categories = await _categoryRepository.GetAllAsync(isNonVeg);
        var exists = categories.Any(c => c.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase) 
                                         && (!excludeId.HasValue || c.Id != excludeId.Value));
        if (exists)
        {
            var typeStr = isNonVeg ? "Non-Veg" : "Veg";
            throw new ConflictException($"Category with name '{name.Trim()}' and type '{typeStr}' already exists.");
        }
    }

    // Marks a category as deleted.
    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("DeleteAsync started for Category ID: {Id}", id);

        var menuItems = await _menuItemRepository.GetAllAsync();
        var hasActiveItems = menuItems.Any(item => item.CategoryId == id && !item.IsDeleted);
        if (hasActiveItems)
        {
            _logger.LogWarning("DeleteAsync failed: Category ID {Id} has active menu items.", id);
            throw new BusinessRuleException("Category cannot be deleted because it contains active menu items.");
        }

        var isDeleted = await _categoryRepository.DeleteAsync(id);
        if (!isDeleted)
        {
            _logger.LogWarning("DeleteAsync failed: Category with ID {Id} was not found.", id);
            throw new NotFoundException($"Category with id {id} was not found.");
        }

        _cache.Remove(CacheKeys.CategoriesAll);
        _cache.Remove(CacheKeys.MenuAll);
        _logger.LogInformation("DeleteAsync succeeded for Category ID: {Id}", id);
        return true;
    }

    private static void ValidateName(string name)
    {
        Validation.RequireNonEmptyString(name, nameof(name), "Category name is required.");
    }

    private static CategoryDto MapCategoryToDto(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            IsNonVeg = category.IsNonVeg,
            IsDeleted = category.IsDeleted
        };
    }
}