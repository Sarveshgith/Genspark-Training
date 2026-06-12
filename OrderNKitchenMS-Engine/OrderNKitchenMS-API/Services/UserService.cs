using System;
using Microsoft.EntityFrameworkCore;
using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Utils;
using Microsoft.Extensions.Logging;

namespace OrderNKitchenMS_API.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    // Retrieves a filtered, paginated list of all users.
    public async Task<IEnumerable<UserDto>> GetAllAsync(QueryUserDto query)
    {
        _logger.LogInformation("GetAllAsync called for users. Search: '{Search}', RoleId: {RoleId}", query?.Search, query?.RoleId);
        var users = await _userRepository.GetAll();

        if (Validation.IsNonEmptyString(query?.Search ?? string.Empty))
        {
            users = users.Where(user => user.Name.Contains(query.Search!) || user.Email.Contains(query.Search!));
        }

        if (query.RoleId.HasValue)
        {
            users = users.Where(user => user.RoleId == query.RoleId.Value);
        }

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        var result = await users
            .OrderBy(user => user.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        _logger.LogInformation("GetAllAsync completed. Returned {Count} users.", result.Count);
        return result.Select(MapUserToDto);
    }

    // Retrieves all available user roles.
    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        _logger.LogInformation("GetAllRolesAsync called");
        var roles = await _userRepository.GetAllRolesAsync();
        return roles.Select(MapRoleToDto);
    }

    // Retrieves a specific user by their unique identifier.
    public async Task<UserDto> GetByIdAsync(int id)
    {
        _logger.LogInformation("GetByIdAsync called for User ID: {Id}", id);
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("GetByIdAsync failed: User with ID {Id} was not found.", id);
            throw new NotFoundException($"User with id {id} was not found.");
        }

        return MapUserToDto(user);
    }

    // Updates an existing user's details.
    public async Task<UserDto> UpdateAsync(int id, UserUpdateDto userUpdateDto)
    {
        _logger.LogInformation("UpdateAsync started for User ID: {Id}", id);
        ValidateUpdateDto(userUpdateDto);
        var userEntity = MapUpdateDtoToEntity(userUpdateDto);
        var updatedUser = await _userRepository.UpdateAsync(id, userEntity);
        if (updatedUser == null)
        {
            _logger.LogWarning("UpdateAsync failed: User with ID {Id} was not found.", id);
            throw new NotFoundException($"User with id {id} was not found.");
        }

        _logger.LogInformation("UpdateAsync succeeded for User ID: {Id}", id);
        return MapUserToDto(updatedUser);
    }

    // Changes the password for a specific user.
    public async Task<bool> ChangePasswordAsync(int id, string hashedPassword)
    {
        _logger.LogInformation("ChangePasswordAsync started for User ID: {Id}", id);
        Validation.RequireNonEmptyString(hashedPassword, nameof(hashedPassword), "Password hash cannot be empty.");

        var isChanged = await _userRepository.ChangePasswordAsync(id, hashedPassword);
        if (!isChanged)
        {
            _logger.LogWarning("ChangePasswordAsync failed: User with ID {Id} was not found.", id);
            throw new NotFoundException($"User with id {id} was not found.");
        }

        _logger.LogInformation("ChangePasswordAsync succeeded for User ID: {Id}", id);
        return true;
    }

    // Marks a specific user as deleted.
    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("DeleteAsync started for User ID: {Id}", id);
        var isDeleted = await _userRepository.DeleteAsync(id);
        if (!isDeleted)
        {
            _logger.LogWarning("DeleteAsync failed: User with ID {Id} was not found.", id);
            throw new NotFoundException($"User with id {id} was not found.");
        }

        _logger.LogInformation("DeleteAsync succeeded for User ID: {Id}", id);
        return true;
    }

    private static UserDto MapUserToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            RoleName = user.Role?.Name.ToString() ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address,
            IsDeleted = user.IsDeleted.ToString()
        };
    }

    private static User MapUpdateDtoToEntity(UserUpdateDto userUpdateDto)
    {
        return new User
        {
            Name = userUpdateDto.Name,
            Email = userUpdateDto.Email,
            PasswordHash = string.Empty,
            PhoneNumber = userUpdateDto.PhoneNumber,
            Address = userUpdateDto.Address,
            RoleId = userUpdateDto.RoleId
        };
    }

    private static RoleDto MapRoleToDto(Role role)
    {
        return new RoleDto
        {
            Id = role.Id,
            RoleValue = (int)role.Name,
            Name = role.Name.ToString()
        };
    }

    private static void ValidateUpdateDto(UserUpdateDto userUpdateDto)
    {
        Validation.RequireNotNull(userUpdateDto, nameof(userUpdateDto), "User data is required.");
        Validation.RequireNonEmptyString(userUpdateDto.Name, nameof(userUpdateDto.Name), "Name is required.");
        Validation.RequireValidEmail(userUpdateDto.Email, nameof(userUpdateDto.Email));
        Validation.RequireValidPhone(userUpdateDto.PhoneNumber, nameof(userUpdateDto.PhoneNumber));
        Validation.RequireNotEmptyIfProvided(userUpdateDto.Address, nameof(userUpdateDto.Address), "If provided, address cannot be empty.");
    }
}
