using System;
using LibraryManagementApp.Models;

namespace LibraryManagementApp.Interfaces;

internal interface IMemberRepository : IRepository<int, Member>
{
    Member? Login(string email, string password);
    Member? GetMember(string? email = null, string? phoneNo = null);
    Member? UpdateStatus(int id);
    List<Member> GetMembersByMembership(int membershipId);
    List<Member> GetMembersWithPendingFines();
}
