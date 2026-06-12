using Microsoft.EntityFrameworkCore;
using OrderNKitchenMS_API.Data;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Repositories.Interfaces;

namespace OrderNKitchenMS_API.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<Category> _categories;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
        _categories = _context.Categories;
    }

    public async Task<IEnumerable<Category>> GetAllAsync(bool? isNonVeg = null)
    {
        var query = _categories.Where(category => !category.IsDeleted);

        if (isNonVeg.HasValue)
        {
            query = query.Where(category => category.IsNonVeg == isNonVeg.Value);
        }

        return await query
            .OrderBy(category => category.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        return await _categories
            .FirstOrDefaultAsync(category => category.Id == id && !category.IsDeleted);
    }

    public async Task<Category> CreateAsync(Category category)
    {
        _categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<Category?> UpdateAsync(int id, Category category)
    {
        var existingCategory = await GetByIdAsync(id);
        if (existingCategory == null)
        {
            return null;
        }

        existingCategory.Name = category.Name;
        existingCategory.IsNonVeg = category.IsNonVeg;
        await _context.SaveChangesAsync();
        return existingCategory;
    }

    //Soft-deleting
    public async Task<bool> DeleteAsync(int id)
    {
        var category = await GetByIdAsync(id);
        if (category == null)
        {
            return false;
        }

        category.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }
}
