using System.Collections.Generic;
using System.Threading.Tasks;
using LibraryManagementAPI.Models.DTOs;

namespace LibraryManagementAPI.Service;

public interface IBookService
{
    public Task<BookDTO> AddBook(CreateBookDTO book);

    public Task<BookDTO> GetBookById(int id);

    public Task<IEnumerable<BookDTO>> GetAllBooks();

    public Task<BookDTO> UpdateBook(int id, UpdateBookDTO book);

    public Task DeleteBook(int id);

    public Task<IEnumerable<BookDTO>> SearchBooks(string title);
}
