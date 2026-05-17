using System;
using LibraryManagementApp.Models;
using LibraryManagementApp.Utils;
using LibraryManagementApp.Models.Exceptions;
using LibraryManagementApp.Repositories;
using LibraryManagementApp.Contexts;

namespace LibraryManagementApp.Services;

internal class MemberService
{
    private readonly MemberRepository memberRepository;
    public MemberService(MemberRepository memberRepository)
    {
        this.memberRepository = memberRepository;
    }

    public void AddMember(Member member)
    {
        if(!ValidationHelper.IsValid(member.Username))
            throw new InvalidArgumentException("Invalid username. It cannot be empty or whitespace.");

        if(!ValidationHelper.IsValid(member.Password))
            throw new InvalidArgumentException("Invalid password. It cannot be empty or whitespace.");

        if(!ValidationHelper.IsValidEmail(member.Email))
            throw new InvalidArgumentException("Invalid email. It cannot be empty or whitespace.");

        if(!ValidationHelper.IsValidPhone(member.PhoneNo))
            throw new InvalidArgumentException("Invalid phone number. It must be a 10-digit number.");

        if(memberRepository.GetMember(email: member.Email) != null || memberRepository.GetMember(phoneNo: member.PhoneNo) != null)
            throw new InvalidArgumentException("User already exists");

        memberRepository.Add(member);
    }

    public Member? Login(string email, string password)
    {
        if(!ValidationHelper.IsValidEmail(email))
            throw new InvalidArgumentException("Invalid email. It cannot be empty or whitespace.");

        if(!ValidationHelper.IsValid(password))
            throw new InvalidArgumentException("Invalid password. It cannot be empty or whitespace.");

        Member? member = memberRepository.Login(email, password);

        if(member == null)
            throw new InvalidArgumentException("Invalid email or password.");
        
        return member;
    }

    public List<Member> GetAllMembers()
    {
        var members = memberRepository.GetAll();
        return members.OrderBy(m => m.Username).ToList();
    }

    public Member? SearchMember(string? email = null, string? phoneNo = null)
    {
        if(!ValidationHelper.IsValidEmail(email) && !ValidationHelper.IsValidPhone(phoneNo))
            throw new InvalidArgumentException("Invalid email or phone number. They cannot be empty or whitespace.");

        return memberRepository.GetMember(email, phoneNo);
    }

    public Member? ToggleMemberStatus(int id)
    {
        var member = memberRepository.Get(id);
        if (member is null)
            throw new InvalidArgumentException("Member not found.");

        return memberRepository.UpdateStatus(id);
    }
}

//Membership check
