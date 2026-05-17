using System.Linq;
using System.Collections.Generic;
using System.Data;
using LibraryManagementApp.Contexts;
using LibraryManagementApp.Interfaces;
using LibraryManagementApp.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementApp.Repositories;

internal class FineRepository : Repository<int, Fine>, IFineRepository
{
	public FineRepository(LibraryDbContext context) : base(context)
	{
	}


	public decimal GetTotalPendingFine(int userId)
	{
		return _context.Database
			.SqlQuery<decimal>(
				$"SELECT calculate_member_fine({userId})"
			)
			.AsEnumerable()
			.FirstOrDefault();
	}

	public List<Fine> GetFineHistory(int userId)
	{
		return _dbSet.Where(f => f.UserId == userId).ToList();
	}

	public void PayFine(int fineId)
	{
		var fine = Get(fineId);
		if (fine is null) return;

		fine.IsPaid = true;
		_context.SaveChanges();
	}
}
