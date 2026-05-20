using System;
using LibraryManagementAPI.Models;
using LibraryManagementAPI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Service;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;

    public BookService(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public ActionResult<Book> AddBook(Book book)
    {
        return _bookRepository.AddBook(book);
    }

    public ActionResult<Book> GetBookById(int id)
    {
        var book = _bookRepository.GetBookById(id).Value;
        if (book == null)
            return new NotFoundObjectResult(new { message = $"Book with ID {id} not found." });
        
        return book;
    }

    public ActionResult<IEnumerable<Book>> GetAllBooks()
    {
        return _bookRepository.GetAllBooks();
    }

    public ActionResult<Book> UpdateBook(int id, Book book)
    {
        var result = _bookRepository.UpdateBook(id, book);
        var updatedBook = result.Value;
        if (updatedBook == null)
            return new NotFoundObjectResult(new { message = $"Book with ID {id} not found." });
        
        return updatedBook;
    }

    public ActionResult DeleteBook(int id)
    {
        return _bookRepository.DeleteBook(id);
    }

    public ActionResult<IEnumerable<Book>> SearchBooks(string title)
    {
        return _bookRepository.SearchBooks(title);
    }
}
