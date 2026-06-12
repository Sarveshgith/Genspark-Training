using OrderNKitchenMS_API.Models.DTOs;

namespace OrderNKitchenMS_API.Services.Interfaces;

public interface IUserService
{
    public Task<IEnumerable<UserDto>> GetAllAsync(QueryUserDto query);

    public Task<IEnumerable<RoleDto>> GetAllRolesAsync();

    public Task<UserDto> GetByIdAsync(int id);

    public Task<UserDto> UpdateAsync(int id, UserUpdateDto userUpdateDto);

    public Task<bool> ChangePasswordAsync(int id, string hashedPassword);

    public Task<bool> DeleteAsync(int id);
}
