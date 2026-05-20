using System;
using LibraryManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Service;

public interface IBookService
{
    public ActionResult<Book> AddBook(Book book);

    public ActionResult<Book> GetBookById(int id);

    public ActionResult<IEnumerable<Book>> GetAllBooks();

    public ActionResult<Book> UpdateBook(int id, Book book);

    public ActionResult DeleteBook(int id);

    public ActionResult<IEnumerable<Book>> SearchBooks(string title);
}
