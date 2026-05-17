using System;
using LibraryManagementApp.Models;

namespace LibraryManagementApp.Interfaces;

internal interface IBookRepository : IRepository<int, Book>
{
    //Book? GetBook(string? title = null, string? author = null, int? categoryId = null);
}
