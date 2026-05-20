using System.Collections.Generic;
using LibraryManagementAPI.Models;
using LibraryManagementAPI.Service;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Book>> GetAll()
    {
        return _bookService.GetAllBooks();
    }

    [HttpGet("{id}")]
    public ActionResult<Book> GetById(int id)
    {
        return _bookService.GetBookById(id);
    }

    [HttpPost]
    public ActionResult<Book> Create([FromBody] Book book)
    {
        var result = _bookService.AddBook(book);
        var created = result.Value;
        return Ok(new {
            message = "Book created successfully",
            data = created
        });
    }

    [HttpPut("{id}")]
    public ActionResult<Book> Update([FromRoute] int id, [FromBody] Book book)
    {
        return _bookService.UpdateBook(id, book);
    }

    [HttpDelete("{id}")]
    public ActionResult Delete([FromRoute] int id)
    {
        return _bookService.DeleteBook(id);
    }

    [HttpGet("search")]
    public ActionResult<IEnumerable<Book>> Search([FromQuery] string title)
    {
        if(string.IsNullOrWhiteSpace(title)){
            return BadRequest("Title query parameter is required.");
        }

        return _bookService.SearchBooks(title);
    }
}
