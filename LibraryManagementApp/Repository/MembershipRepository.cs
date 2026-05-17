using System.Linq;
using LibraryManagementApp.Contexts;
using LibraryManagementApp.Enums;
using LibraryManagementApp.Interfaces;
using LibraryManagementApp.Models;

namespace LibraryManagementApp.Repositories;

internal class MembershipRepository : Repository<int, Membership>, IMembershipRepository
{
	public MembershipRepository(LibraryDbContext context) : base(context)
	{
	}

	public Membership? GetByType(MembershipType type)
	{
		return _dbSet.FirstOrDefault(m => m.Type == type);
	}
}
