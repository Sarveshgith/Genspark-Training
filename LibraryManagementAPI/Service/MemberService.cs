using System;
using LibraryManagementAPI.Models;
using LibraryManagementAPI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Service;

public class MemberService : IMemberService
{
    private readonly IMemberRepository _memberRepository;

    public MemberService(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public ActionResult<Member> AddMember(Member member)
    {
        member.MembershipDate = NormalizeMembershipDate(member.MembershipDate);
        return _memberRepository.AddMember(member);
    }

    public ActionResult<Member> GetMemberById(int id)
    {
        var member = _memberRepository.GetMemberById(id).Value;
        if (member == null)
            return new NotFoundObjectResult(new { message = $"Member with ID {id} not found." });
        
        return member;
    }

    public ActionResult<IEnumerable<Member>> GetAllMembers()
    {
        return _memberRepository.GetAllMembers();
    }

    private static DateTime NormalizeMembershipDate(DateTime membershipDate)
    {
        if (membershipDate == default)
            return DateTime.UtcNow;

        return membershipDate.Kind switch
        {
            DateTimeKind.Utc => membershipDate,
            DateTimeKind.Local => membershipDate.ToUniversalTime(),
            _ => DateTime.SpecifyKind(membershipDate, DateTimeKind.Utc)
        };
    }
}
