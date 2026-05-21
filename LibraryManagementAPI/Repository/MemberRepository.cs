using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibraryManagementAPI.Data;
using LibraryManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementAPI.Repository;

public class MemberRepository : IMemberRepository
{
    private readonly LibraryDbContext _context;

    public MemberRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<Member> AddMember(Member member)
    {
        var duplicateEmailExists = await _context.Members.AnyAsync(existingMember => existingMember.Email == member.Email);
        if (duplicateEmailExists)
            throw new InvalidOperationException($"Member with email {member.Email} already exists.");

        var duplicatePhoneExists = await _context.Members.AnyAsync(existingMember => existingMember.PhoneNo == member.PhoneNo);
        if (duplicatePhoneExists)
            throw new InvalidOperationException($"Member with phone number {member.PhoneNo} already exists.");

        await _context.Members.AddAsync(member);
        await _context.SaveChangesAsync();
        return member;
    }

    public async Task<Member> GetMemberById(int id)
    {
        var member = await _context.Members.FindAsync(id);
        if (member == null)
            throw new InvalidOperationException($"Member with ID {id} not found.");

        return member;
    }

    public async Task<IEnumerable<Member>> GetAllMembers()
    {
        return await _context.Members.ToListAsync();
    }
}
