using LibraryManagementAPI.Models.DTOs;

namespace LibraryManagementAPI.Service;

public interface IMemberService
{
    public Task<MemberDTO> AddMember(CreateMemberDTO member);

    public Task<MemberDTO> GetMemberById(int id);

    public Task<IEnumerable<MemberDTO>> GetAllMembers();
}
