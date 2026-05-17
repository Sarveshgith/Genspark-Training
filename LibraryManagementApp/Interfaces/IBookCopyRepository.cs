using LibraryManagementApp.Models;
using LibraryManagementApp.Enums;

namespace LibraryManagementApp.Interfaces;

internal interface IBookCopyRepository : IRepository<int, BookCopy>
{
    List<BookCopy> GetAvailableCopies(int bookId);

    BookCopy? GetAvailableCopy(int bookId);

    List<BookCopy> GetDamagedCopies();

    void UpdateStatus(int copyId, BookCopyStatus status);
}