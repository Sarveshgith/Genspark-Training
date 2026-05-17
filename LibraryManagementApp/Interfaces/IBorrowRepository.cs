using System;
using LibraryManagementApp.Models;

namespace LibraryManagementApp.Interfaces;

internal interface IBorrowRepository : IRepository<int, Borrow>
{
    List<Borrow> GetActiveBorrowings(int userId);

    List<Borrow> GetBorrowHistory(int userId);

    List<Borrow> GetOverdueBorrowings();

    Borrow? GetActiveBorrowByBookCopy(int copyId);

    bool HasActiveBorrowing(int userId, int bookId);
}
