using System;
using System.Linq;
using LibraryManagementApp.Contexts;
using LibraryManagementApp.Enums;
using LibraryManagementApp.Interfaces;
using LibraryManagementApp.Models;

namespace LibraryManagementApp.Repositories;

internal class MemberRepository : Repository<int, Member>, IMemberRepository
{
    public MemberRepository(LibraryDbContext context) : base(context)
    {
    }

    public override void Add(Member entity)
    {
        if (string.IsNullOrEmpty(entity.Username) || string.IsNullOrEmpty(entity.Email) || string.IsNullOrEmpty(entity.PhoneNo))
        {
            throw new ArgumentException("Username, Email and Phone number are required.");
        }

        base.Add(entity);
    }

    public Member? Login(string email, string password)
    {
        Member? member = _dbSet.Where(m => m.Email == email && m.Password == password).FirstOrDefault();
        if(member != null && member.Status == MemberStatus.Active)
        {
            return member;
        }
        return null;
    }

    public Member? GetMember(string? email = null, string? phoneNo = null)
    {
        if (!string.IsNullOrEmpty(email))
            return _dbSet.FirstOrDefault(m => m.Email == email);

        if (!string.IsNullOrEmpty(phoneNo))
            return _dbSet.FirstOrDefault(m => m.PhoneNo == phoneNo);

        return null;
    }

    public Member? UpdateStatus(int id)
    {
        var member = Get(id);
        if (member is null) return null;

        member.Status = (member.Status == MemberStatus.Active) ? MemberStatus.Inactive : MemberStatus.Active;
        _context.SaveChanges();
        return member;
    }

    public List<Member> GetMembersByMembership(int membershipId)
    {
        return _dbSet.Where(m => m.MembershipId == membershipId).ToList();
    }

    public List<Member> GetMembersWithPendingFines()
    {
        return _dbSet.Where(m => _context.Fines.Any(f => !f.IsPaid && f.UserId == m.Id))
            .ToList();
    }
}
