using System.Linq;
using System.Collections.Generic;
using LibraryManagementApp.Contexts;
using LibraryManagementApp.Enums;
using LibraryManagementApp.Interfaces;
using LibraryManagementApp.Models;

namespace LibraryManagementApp.Repositories;

internal class BookCopyRepository : Repository<int, BookCopy>, IBookCopyRepository
{
	public BookCopyRepository(LibraryDbContext context) : base(context)
	{
	}

	public List<BookCopy> GetAvailableCopies(int bookId)
	{
		return _dbSet.Where(bc => bc.BookId == bookId && bc.Status == BookCopyStatus.Available).ToList();
	}

	public BookCopy? GetAvailableCopy(int bookId)
	{
		return _dbSet.FirstOrDefault(bc => bc.BookId == bookId && bc.Status == BookCopyStatus.Available);
	}

	public List<BookCopy> GetDamagedCopies()
	{
		return _dbSet.Where(bc => bc.Status == BookCopyStatus.Damaged).ToList();
	}

	public void UpdateStatus(int copyId, BookCopyStatus status)
	{
		var copy = Get(copyId);
		if (copy is null) return;

		copy.Status = status;
		_context.SaveChanges();
	}
}
