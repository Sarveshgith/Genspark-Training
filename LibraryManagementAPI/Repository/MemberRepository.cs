using System;
using LibraryManagementAPI.Data;
using LibraryManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Repository;

public class MemberRepository : IMemberRepository
{
    private readonly LibraryDbContext _context;

    public MemberRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public ActionResult<Member> AddMember(Member member)
    {
        var duplicateEmailExists = _context.Members.Any(existingMember => existingMember.Email == member.Email);
        if (duplicateEmailExists)
            return new ConflictObjectResult(new { message = $"Member with email {member.Email} already exists." });

        var duplicatePhoneExists = _context.Members.Any(existingMember => existingMember.PhoneNo == member.PhoneNo);
        if (duplicatePhoneExists)
            return new ConflictObjectResult(new { message = $"Member with phone number {member.PhoneNo} already exists." });

        _context.Members.Add(member);
        _context.SaveChanges();
        return member;
    }

    public ActionResult<Member> GetMemberById(int id)
    {
        var member = _context.Members.Find(id);
        return member;
    }

    public ActionResult<IEnumerable<Member>> GetAllMembers()
    {
        return _context.Members.ToList();
    }
}
