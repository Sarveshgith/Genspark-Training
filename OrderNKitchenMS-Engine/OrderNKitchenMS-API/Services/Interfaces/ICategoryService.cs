using OrderNKitchenMS_API.Models.DTOs;

namespace OrderNKitchenMS_API.Services.Interfaces;

public interface ICategoryService
{
    public Task<IEnumerable<CategoryDto>> GetAllAsync(bool? isNonVeg = null);

    public Task<CategoryDto> GetByIdAsync(int id);

    public Task<CategoryDto> CreateAsync(CategoryCreateDto categoryCreateDto);

    public Task<CategoryDto> UpdateAsync(int id, CategoryUpdateDto categoryUpdateDto);

    public Task<bool> DeleteAsync(int id);
}