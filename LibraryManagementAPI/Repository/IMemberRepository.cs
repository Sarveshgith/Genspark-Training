using System;
using LibraryManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Repository;

public interface IMemberRepository
{
    public ActionResult<Member> AddMember(Member member);

    public ActionResult<Member> GetMemberById(int id);

    public ActionResult<IEnumerable<Member>> GetAllMembers();
}
