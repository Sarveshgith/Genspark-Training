using System;
using LibraryManagementApp.Models;

namespace LibraryManagementApp.Interfaces;

internal interface IFineRepository : IRepository<int, Fine>
{
    decimal GetTotalPendingFine(int userId);

    List<Fine> GetFineHistory(int userId);

    void PayFine(int fineId);
}