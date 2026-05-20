using System.Collections.Generic;
using LibraryManagementAPI.Models;
using LibraryManagementAPI.Service;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementAPI.Controllers;

[ApiController]
[Route("api/members")]
public class MemberController : ControllerBase
{
    private readonly IMemberService _memberService;

    public MemberController(IMemberService memberService)
    {
        _memberService = memberService;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Member>> GetAll()
    {
        return _memberService.GetAllMembers();
    }

    [HttpGet("{id}")]
    public ActionResult<Member> GetById([FromRoute] int id)
    {
        return _memberService.GetMemberById(id);
    }

    [HttpPost]
    public ActionResult<Member> Create([FromBody]Member member)
    {
        var result = _memberService.AddMember(member);
        var created = result.Value;
        return Ok(new {
            message = "Member created successfully",
            data = created
        });
    }
}
