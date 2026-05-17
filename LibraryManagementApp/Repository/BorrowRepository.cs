using System.Linq;
using System.Collections.Generic;
using LibraryManagementApp.Contexts;
using LibraryManagementApp.Enums;
using LibraryManagementApp.Interfaces;
using LibraryManagementApp.Models;

namespace LibraryManagementApp.Repositories;

internal class BorrowRepository : Repository<int, Borrow>, IBorrowRepository
{
	public BorrowRepository(LibraryDbContext context) : base(context)
	{
	}

	public List<Borrow> GetActiveBorrowings(int userId)
	{
		return _dbSet.Where(b => b.UserId == userId && b.Status != BorrowStatus.Returned).ToList();
	}

	public List<Borrow> GetBorrowHistory(int userId)
	{
		return _dbSet.Where(b => b.UserId == userId).ToList();
	}

	public List<Borrow> GetOverdueBorrowings()
	{
		return _dbSet.Where(b => b.DueDate < DateTime.UtcNow && b.Status != BorrowStatus.Returned).ToList();
	}

	public Borrow? GetActiveBorrowByBookCopy(int copyId)
	{
		return _dbSet.FirstOrDefault(b => b.BookCopyId == copyId && b.Status != BorrowStatus.Returned);
	}

	public bool HasActiveBorrowing(int userId, int bookId)
	{
		return _dbSet.Any(b => b.UserId == userId && b.BookId == bookId && b.Status != BorrowStatus.Returned);
	}
}
