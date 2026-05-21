using LibraryManagementAPI.Models;
using LibraryManagementAPI.Models.DTOs;
using LibraryManagementAPI.Repository;

namespace LibraryManagementAPI.Service;

public class MemberService : IMemberService
{
    private readonly IMemberRepository _memberRepository;

    public MemberService(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<MemberDTO> AddMember(CreateMemberDTO memberDto)
    {
        var member = MapCreateMemberDtoToMember(memberDto);

        var createdMember = await _memberRepository.AddMember(member);
        return MapMemberToDto(createdMember);
    }

    public async Task<MemberDTO> GetMemberById(int id)
    {
        var member = await _memberRepository.GetMemberById(id);
        return MapMemberToDto(member);
    }

    public async Task<IEnumerable<MemberDTO>> GetAllMembers()
    {
        var members = await _memberRepository.GetAllMembers();
        return members.Select(MapMemberToDto).ToList();
    }

    private static Member MapCreateMemberDtoToMember(CreateMemberDTO memberDto)
    {
        return new Member
        {
            FullName = memberDto.FullName,
            Email = memberDto.Email,
            PhoneNo = memberDto.PhoneNo,
            MembershipDate = memberDto.MembershipDate
        };
    }

    private static MemberDTO MapMemberToDto(Member member)
    {
        return new MemberDTO
        {
            MemberId = member.MemberId,
            FullName = member.FullName,
            Email = member.Email,
            PhoneNo = member.PhoneNo,
            MembershipDate = member.MembershipDate
        };
    }
}
