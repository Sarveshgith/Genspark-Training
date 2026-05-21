using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibraryManagementAPI.Models;
using LibraryManagementAPI.Models.DTOs;
using LibraryManagementAPI.Repository;

namespace LibraryManagementAPI.Service;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;

    public BookService(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<BookDTO> AddBook(CreateBookDTO bookDto)
    {
        var book = MapCreateBookDtoToBook(bookDto);
        var createdBook = await _bookRepository.AddBook(book);
        return MapBookToDto(createdBook);
    }

    public async Task<BookDTO> GetBookById(int id)
    {
        var book = await _bookRepository.GetBookById(id);
        return MapBookToDto(book);
    }

    public async Task<IEnumerable<BookDTO>> GetAllBooks()
    {
        var books = await _bookRepository.GetAllBooks();
        return books.Select(MapBookToDto).ToList();
    }

    public async Task<BookDTO> UpdateBook(int id, UpdateBookDTO bookDto)
    {
        var book = MapUpdateBookDtoToBook(bookDto);
        var updatedBook = await _bookRepository.UpdateBook(id, book);
        return MapBookToDto(updatedBook);
    }

    public async Task DeleteBook(int id)
    {
        await _bookRepository.DeleteBook(id);
    }

    public async Task<IEnumerable<BookDTO>> SearchBooks(string title)
    {
        var books = await _bookRepository.SearchBooks(title);
        return books.Select(MapBookToDto).ToList();
    }

    private static Book MapCreateBookDtoToBook(CreateBookDTO bookDto)
    {
        return new Book
        {
            Title = bookDto.Title,
            Author = bookDto.Author,
            ISBN = bookDto.ISBN,
            PublicationYear = bookDto.PublicationYear,
            AvailableCopies = bookDto.AvailableCopies
        };
    }

    private static Book MapUpdateBookDtoToBook(UpdateBookDTO bookDto)
    {
        return new Book
        {
            Title = bookDto.Title,
            Author = bookDto.Author,
            ISBN = bookDto.ISBN,
            PublicationYear = bookDto.PublicationYear,
            AvailableCopies = bookDto.AvailableCopies
        };
    }

    private static BookDTO MapBookToDto(Book book)
    {
        return new BookDTO
        {
            BookId = book.BookId,
            Title = book.Title,
            Author = book.Author,
            ISBN = book.ISBN,
            PublicationYear = book.PublicationYear,
            AvailableCopies = book.AvailableCopies
        };
    }
}
