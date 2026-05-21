using System.Collections.Generic;
using System.Threading.Tasks;
using LibraryManagementAPI.Models;

namespace LibraryManagementAPI.Repository;

public interface IMemberRepository
{
    public Task<Member> AddMember(Member member);

    public Task<Member> GetMemberById(int id);

    public Task<IEnumerable<Member>> GetAllMembers();

}
