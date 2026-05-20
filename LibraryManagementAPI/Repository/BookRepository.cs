using System;
using LibraryManagementAPI.Data;
using LibraryManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Repository;

public class BookRepository : IBookRepository
{
    private readonly LibraryDbContext _context;

    public BookRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public ActionResult<Book> AddBook(Book book)
    {
        if (_context.Books.Any(existingBook => existingBook.ISBN == book.ISBN))
            return new ConflictObjectResult(new { message = $"Book with ISBN {book.ISBN} already exists." });

        _context.Books.Add(book);
        _context.SaveChanges();
        return book;
    }

    public ActionResult<Book> GetBookById(int id)
    {
        var book = _context.Books.Find(id);
        return book;
    }

    public ActionResult<IEnumerable<Book>> GetAllBooks()
    {
        return _context.Books.ToList();
    }

    public ActionResult<Book> UpdateBook(int id, Book book)
    {
        var existingBook = _context.Books.Find(id);
        if (existingBook == null)
            return null;

        var duplicateIsbnExists = _context.Books.Any(candidate => candidate.BookId != id && candidate.ISBN == book.ISBN);
        if (duplicateIsbnExists)
            return new ConflictObjectResult(new { message = $"Book with ISBN {book.ISBN} already exists." });
        
        existingBook.Title = book.Title;
        existingBook.Author = book.Author;
        existingBook.ISBN = book.ISBN;
        existingBook.PublicationYear = book.PublicationYear;
        existingBook.AvailableCopies = book.AvailableCopies;

        _context.SaveChanges();
        return existingBook;
    }

    public ActionResult DeleteBook(int id)
    {
        var book = _context.Books.Find(id);
        if (book == null)
            return new NotFoundResult();
        
        _context.Books.Remove(book);
        _context.SaveChanges();
        return new NoContentResult();
    }

    public ActionResult<IEnumerable<Book>> SearchBooks(string title)
    {
        return _context.Books.Where(b => b.Title.Contains(title)).ToList();
    }
}
