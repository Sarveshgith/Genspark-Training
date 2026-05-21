using LibraryManagementAPI.Models.DTOs;
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
    public async Task<ActionResult<IEnumerable<BookDTO>>> GetAll()
    {
        try
        {
            var books = await _bookService.GetAllBooks();
            return Ok(books);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookDTO>> GetById(int id)
    {
        try
        {
            var book = await _bookService.GetBookById(id);
            return Ok(book);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<BookDTO>> Create([FromBody] CreateBookDTO bookDto)
    {
        try
        {
            var book = await _bookService.AddBook(bookDto);
            return Ok(new {
                message = "Book created successfully",
                book = book
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BookDTO>> Update(int id, [FromBody] UpdateBookDTO bookDto)
    {
        try
        {
            var book = await _bookService.UpdateBook(id, bookDto);
            return Ok(new {
                message = "Book updated successfully",
                book = book
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await _bookService.DeleteBook(id);
            return Ok(new { message = "Book deleted successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<BookDTO>>> Search([FromQuery] string title)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(title))
                return BadRequest(new { message = "Title query parameter is required." });

            return Ok(await _bookService.SearchBooks(title));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}