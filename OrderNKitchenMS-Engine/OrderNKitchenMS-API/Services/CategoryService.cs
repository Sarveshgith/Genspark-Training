using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Utils;
using Microsoft.Extensions.Logging;

namespace OrderNKitchenMS_API.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(ICategoryRepository categoryRepository, ILogger<CategoryService> logger)
    {
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    // Retrieves all categories, optionally filtered by food type (veg/non-veg).
    public async Task<IEnumerable<CategoryDto>> GetAllAsync(bool? isNonVeg = null)
    {
        _logger.LogInformation("GetAllAsync called with isNonVeg: {IsNonVeg}", isNonVeg);
        var categories = await _categoryRepository.GetAllAsync(isNonVeg);
        _logger.LogInformation("GetAllAsync completed. Returned {Count} categories.", categories.Count());
        return categories.Select(MapCategoryToDto);
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

        var createdCategory = await _categoryRepository.CreateAsync(new Category
        {
            Name = categoryCreateDto.Name.Trim(),
            IsNonVeg = categoryCreateDto.IsNonVeg
        });

        _logger.LogInformation("CreateAsync succeeded. Created Category ID: {Id}", createdCategory.Id);
        return MapCategoryToDto(createdCategory);
    }

    // Updates the details of an existing category.
    public async Task<CategoryDto> UpdateAsync(int id, CategoryUpdateDto categoryUpdateDto)
    {
        _logger.LogInformation("UpdateAsync started for Category ID: {Id}", id);
        Validation.RequireNotNull(categoryUpdateDto, nameof(categoryUpdateDto), "Category data is required.");
        ValidateName(categoryUpdateDto.Name);

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

        _logger.LogInformation("UpdateAsync succeeded for Category ID: {Id}", id);
        return MapCategoryToDto(updatedCategory);
    }

    // Marks a category as deleted.
    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("DeleteAsync started for Category ID: {Id}", id);
        var isDeleted = await _categoryRepository.DeleteAsync(id);
        if (!isDeleted)
        {
            _logger.LogWarning("DeleteAsync failed: Category with ID {Id} was not found.", id);
            throw new NotFoundException($"Category with id {id} was not found.");
        }

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