using System;
using System.Linq;
using System.Collections.Generic;
using LibraryManagementApp.Models;
using LibraryManagementApp.Models.Exceptions;
using LibraryManagementApp.Interfaces;
using LibraryManagementApp.Utils;
using LibraryManagementApp.Enums;

namespace LibraryManagementApp.Services;

internal class BookService
{
	private readonly IBookRepository _bookRepo;
	private readonly IBookCopyRepository _copyRepo;
	private readonly ICategoryRepository _categoryRepo;

	public BookService(IBookRepository bookRepo, IBookCopyRepository copyRepo, ICategoryRepository categoryRepo)
	{
		_bookRepo = bookRepo;
		_copyRepo = copyRepo;
		_categoryRepo = categoryRepo;
	}

	public void AddBook(Book book)
	{
		if (!ValidationHelper.IsValid(book.Title))
			throw new InvalidArgumentException("Invalid title.");

		if (!ValidationHelper.IsValid(book.Author))
			throw new InvalidArgumentException("Invalid author.");

		if (!ValidationHelper.IsValidISBN(book.ISBN))
			throw new InvalidArgumentException("Invalid ISBN. It must be a valid ISBN format.");

		if (book.CategoryId <= 0)
			throw new InvalidArgumentException("Invalid category ID. It must be a positive number.");

		var category = _categoryRepo.Get(book.CategoryId);
		if (category is null)
			throw new NotFoundException("Category does not exist.");

		// Check duplicate ISBN
		if (_bookRepo.GetAll().Any(b => b.ISBN == book.ISBN))
			throw new ConflictException("Book with same ISBN already exists.");

		_bookRepo.Add(book);
	}

	public void AddBookCopies(int bookId, int count)
	{
		if (bookId <= 0)
			throw new InvalidArgumentException("Invalid book ID. It must be a positive number.");

		if (count <= 0)
			throw new InvalidArgumentException("Count must be a positive number.");

		var book = _bookRepo.Get(bookId);
		if (book is null)
			throw new NotFoundException("Book not found.");

		for (int i = 0; i < count; i++)
		{
			var copy = new BookCopy
			{
				BookId = bookId,
				Status = BookCopyStatus.Available,
				CreatedAt = DateTime.UtcNow
			};

			_copyRepo.Add(copy);
		}
	}

	public List<Book> GetAllBooks()
	{
		var books = _bookRepo.GetAll();
		return books.OrderBy(b => b.Title).ToList();
	}

	public List<Book> SearchBook(string? title = null, string? author = null, int? categoryId = null)
	{
		if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(author) && !categoryId.HasValue)
			throw new InvalidArgumentException("At least one search parameter must be provided.");

		var books = _bookRepo.GetAll().AsQueryable();

		if (!string.IsNullOrEmpty(title))
			books = books.Where(b => b.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

		if (!string.IsNullOrEmpty(author))
			books = books.Where(b => b.Author.Contains(author, StringComparison.OrdinalIgnoreCase));

		if (categoryId.HasValue && categoryId > 0)
			books = books.Where(b => b.CategoryId == categoryId.Value);

		return books.ToList();
	}

	public List<BookCopy> GetAvailableCopies(int bookId)
	{
		if (bookId <= 0)
			throw new InvalidArgumentException("Invalid book ID. It must be a positive number.");

		var book = _bookRepo.Get(bookId);
		if (book is null)
			throw new InvalidArgumentException("Book not found.");

		return _copyRepo.GetAvailableCopies(bookId);
	}

	public void UpdateBookStatus(int copyId, BookCopyStatus status)
	{
		if (copyId <= 0)
			throw new InvalidArgumentException("Invalid copy ID. It must be a positive number.");

		var copy = _copyRepo.Get(copyId);
		if (copy is null)
			throw new NotFoundException("Book copy not found.");

		_copyRepo.UpdateStatus(copyId, status);
	}

	public List<Book> GetBooksByCategory(int categoryId)
	{
		if (categoryId <= 0)
			throw new InvalidArgumentException("Invalid category ID. It must be a positive number.");

		var category = _categoryRepo.Get(categoryId);
		if (category is null)
			throw new NotFoundException("Category does not exist.");

		var books = _bookRepo.GetAll().Where(b => b.CategoryId == categoryId).OrderBy(b => b.Title).ToList();
		return books;
	}
}