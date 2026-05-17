using System.Linq;
using LibraryManagementApp.Contexts;
using LibraryManagementApp.Interfaces;
using LibraryManagementApp.Models;

namespace LibraryManagementApp.Repositories;

internal class BookRepository : Repository<int, Book>, IBookRepository
{
	public BookRepository(LibraryDbContext context) : base(context)
	{
	}

	// public Book? GetBook(string? title = null, string? author = null, int? categoryId = null)
	// {
	// 	var query = _dbSet.AsQueryable();

	// 	if (!string.IsNullOrEmpty(title))
	// 		query = query.Where(b => b.Title.Contains(title));

	// 	if (!string.IsNullOrEmpty(author))
	// 		query = query.Where(b => b.Author.Contains(author));

	// 	if (categoryId.HasValue)
	// 		query = query.Where(b => b.CategoryId == categoryId.Value);

	// 	return query.FirstOrDefault();
	// }
}
