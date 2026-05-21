using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LibraryManagementAPI.Models.DTOs;
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
    public async Task<ActionResult<IEnumerable<MemberDTO>>> GetAll()
    {
        try
        {
            return Ok(await _memberService.GetAllMembers());
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MemberDTO>> GetById([FromRoute] int id)
    {
        try
        {
            return Ok(await _memberService.GetMemberById(id));
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<MemberDTO>> Create([FromBody] CreateMemberDTO memberDto)
    {
        try
        {
            var member = await _memberService.AddMember(memberDto);
            return Ok(new {
                message = "Member created successfully",
                member = member
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
