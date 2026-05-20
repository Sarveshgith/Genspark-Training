using System;
using LibraryManagementAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Service;

public interface IMemberService
{
    public ActionResult<Member> AddMember(Member member);

    public ActionResult<Member> GetMemberById(int id);

    public ActionResult<IEnumerable<Member>> GetAllMembers();
}
