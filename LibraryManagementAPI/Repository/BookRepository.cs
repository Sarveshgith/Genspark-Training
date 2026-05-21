using LibraryManagementAPI.Data;
using LibraryManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementAPI.Repository;

public class BookRepository : IBookRepository
{
    private readonly LibraryDbContext _context;

    public BookRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<Book> AddBook(Book book)
    {
        await _context.Books.AddAsync(book);
        await _context.SaveChangesAsync();
        return book;
    }

    public async Task<Book> GetBookById(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book == null)
            throw new InvalidOperationException($"Book with ID {id} not found.");

        return book;
    }

    public async Task<IEnumerable<Book>> GetAllBooks()
    {
        return await _context.Books.ToListAsync();
    }

    public async Task<Book> UpdateBook(int id, Book book)
    {
        var existingBook = await _context.Books.FindAsync(id);
        if (existingBook == null)
            throw new InvalidOperationException($"Book with ID {id} not found.");

        existingBook.Title = book.Title;
        existingBook.Author = book.Author;
        existingBook.ISBN = book.ISBN;
        existingBook.PublicationYear = book.PublicationYear;
        existingBook.AvailableCopies = book.AvailableCopies;

        await _context.SaveChangesAsync();
        return existingBook;
    }

    public async Task DeleteBook(int id)
    {
        var book = await _context.Books.FindAsync(id);
        if (book == null)
            throw new InvalidOperationException($"Book with ID {id} not found.");

        _context.Books.Remove(book);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Book>> SearchBooks(string title)
    {
        return await _context.Books.Where(b => b.Title.Contains(title)).ToListAsync();
    }
}
