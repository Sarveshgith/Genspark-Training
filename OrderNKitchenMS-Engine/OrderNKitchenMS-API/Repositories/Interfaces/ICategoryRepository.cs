using OrderNKitchenMS_API.Models.Entities;

namespace OrderNKitchenMS_API.Repositories.Interfaces;

public interface ICategoryRepository
{
    public Task<IEnumerable<Category>> GetAllAsync(bool? isNonVeg = null);

    public Task<Category?> GetByIdAsync(int id);

    public Task<Category> CreateAsync(Category category);

    public Task<Category?> UpdateAsync(int id, Category category);

    public Task<bool> DeleteAsync(int id);
}
