using System.Linq;
using LibraryManagementApp.Contexts;
using LibraryManagementApp.Interfaces;
using LibraryManagementApp.Models;

namespace LibraryManagementApp.Repositories;

internal class CategoryRepository : Repository<int, Category>, ICategoryRepository
{
	public CategoryRepository(LibraryDbContext context) : base(context)
	{
	}

	public Category? GetCategoryByName(string name)
	{
		return _dbSet.FirstOrDefault(c => c.CategoryName == name);
	}

	public List<Category> GetAllCategories()
	{
		return _dbSet.OrderBy(c => c.CategoryName).ToList();
	}
}
