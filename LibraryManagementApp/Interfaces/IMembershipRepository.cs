using System;
using LibraryManagementApp.Models;
using LibraryManagementApp.Enums;

namespace LibraryManagementApp.Interfaces;

internal interface IMembershipRepository : IRepository<int, Membership>
{
    Membership? GetByType(MembershipType type);
}
