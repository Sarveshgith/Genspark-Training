using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Utils;
using Microsoft.Extensions.Logging;

namespace OrderNKitchenMS_API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ITokenService tokenService, ILogger<UserController> logger)
    {
        _userService = userService;
        _tokenService = tokenService;
        _logger = logger;
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UserUpdateDto userUpdateDto)
    {
        Validation.ValidateRequest(id, userUpdateDto);
        _logger.LogInformation("UpdateUser requested for ID: {Id}", id);
        var updatedUser = await _userService.UpdateAsync(id, userUpdateDto);
        _logger.LogInformation("UpdateUser completed for ID: {Id}", id);
        return Ok(updatedUser);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPatch("{id:int}/approve")]
    public async Task<ActionResult<UserDto>> ApproveUser(int id)
    {
        Validation.ValidateId(id);
        _logger.LogInformation("ApproveUser requested for ID: {Id}", id);
        var approvedUser = await _userService.ApproveUserAsync(id);
        _logger.LogInformation("ApproveUser completed for ID: {Id}", id);
        return Ok(approvedUser);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPatch("{id:int}/role")]
    public async Task<ActionResult<UserDto>> UpdateUserRole(int id, [FromBody] UserRoleUpdateDto roleUpdateDto)
    {
        Validation.ValidateRequest(id, roleUpdateDto);
        var currentUserId = User.GetUserId();

        _logger.LogInformation("UpdateUserRole requested for ID: {Id} to Role ID: {RoleId}", id, roleUpdateDto.RoleId);
        var updatedUser = await _userService.UpdateUserRoleAsync(id, roleUpdateDto.RoleId, currentUserId);
        _logger.LogInformation("UpdateUserRole completed for ID: {Id}", id);
        return Ok(updatedUser);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers([FromQuery] QueryUserDto query)
    {
        _logger.LogInformation("GetUsers requested. Search: '{Search}', RoleId: {RoleId}", query?.Search, query?.RoleId);
        var users = await _userService.GetAllAsync(query ?? new QueryUserDto());
        _logger.LogInformation("GetUsers completed. Returned {Count} users.", users.Count());
        return Ok(users);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
    {
        _logger.LogInformation("GetRoles requested");
        var roles = await _userService.GetAllRolesAsync();
        _logger.LogInformation("GetRoles completed. Returned {Count} roles.", roles.Count());
        return Ok(roles);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetUserById(int id)
    {
        Validation.ValidateId(id);
        _logger.LogInformation("GetUserById requested for ID: {Id}", id);
        var user = await _userService.GetByIdAsync(id);
        _logger.LogInformation("GetUserById completed for ID: {Id}", id);
        return Ok(user);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var userId = User.GetUserId();
        _logger.LogInformation("GetMe requested. Extracted UserId: {UserId}", userId);
        var user = await _userService.GetByIdAsync(userId);
        _logger.LogInformation("GetMe completed. Returned user profile for UserId: {UserId}", userId);
        return Ok(user);
    }

    [HttpPatch("{id:int}/password")]
    public async Task<ActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto request)
    {
        Validation.ValidateRequest(id, request);
        _logger.LogInformation("ChangePassword requested for User ID: {Id}", id);
        Validation.RequireNonEmptyString(request.NewPassword, nameof(request.NewPassword), "Password is required.");
        Validation.RequireStrongPassword(request.NewPassword, nameof(request.NewPassword));

        var currentUserId = User.GetUserId();
        if (currentUserId != id)
        {
            _logger.LogWarning("ChangePassword forbidden: Current User ID {CurrentUserId} does not match Target User ID {TargetId}", currentUserId, id);
            throw new ForbiddenException("You are not authorized to change another user's password.");
        }

        var hashedPassword = _tokenService.HashPassword(request.NewPassword);
        await _userService.ChangePasswordAsync(id, hashedPassword);
        _logger.LogInformation("ChangePassword succeeded for User ID: {Id}", id);
        return NoContent();
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteUser(int id)
    {
        Validation.ValidateId(id);
        _logger.LogInformation("DeleteUser requested for ID: {Id}", id);
        await _userService.DeleteAsync(id);
        _logger.LogInformation("DeleteUser completed for ID: {Id}", id);
        return NoContent();
    }
}