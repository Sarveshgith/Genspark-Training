using LibraryManagementAPI.Models;

namespace LibraryManagementAPI.Repository;

public interface IBookRepository
{
    public Task<Book> AddBook(Book book);

    public Task<Book> GetBookById(int id);

    public Task<IEnumerable<Book>> GetAllBooks();

    public Task<Book> UpdateBook(int id, Book book);

    public Task DeleteBook(int id);

    public Task<IEnumerable<Book>> SearchBooks(string title);
}
