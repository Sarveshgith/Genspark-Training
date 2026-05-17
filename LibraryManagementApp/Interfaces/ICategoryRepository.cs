using System;
using LibraryManagementApp.Models;

namespace LibraryManagementApp.Interfaces;

internal interface ICategoryRepository : IRepository<int, Category>
{
    Category? GetCategoryByName(string name);
    List<Category> GetAllCategories();
}
