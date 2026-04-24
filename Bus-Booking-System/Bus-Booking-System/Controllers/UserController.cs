using Bus_Booking_System.Data;
using Bus_Booking_System.Models.DTOs;
using Bus_Booking_System.Models.Entities;
using Bus_Booking_System.Models.Enums;
using Bus_Booking_System.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bus_Booking_System.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UserController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAllUsers()
    {
        var users = await dbContext.Users
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => ToResponse(x))
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetUserById(Guid id)
    {
        if (!IsSelfOrAdmin(id))
        {
            return Forbid();
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound("User not found.");
        }

        return Ok(ToResponse(user));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserResponse>> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        if (!IsSelfOrAdmin(id))
        {
            return Forbid();
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound("User not found.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var phone = request.Phone.Trim();

        var emailExists = await dbContext.Users.AnyAsync(x => x.Email == email && x.Id != id);
        if (emailExists)
        {
            return Conflict("Email already exists.");
        }

        var phoneExists = await dbContext.Users.AnyAsync(x => x.Phone == phone && x.Id != id);
        if (phoneExists)
        {
            return Conflict("Phone already exists.");
        }

        user.Name = request.Name.Trim();
        user.Email = email;
        user.Phone = phone;

        await dbContext.SaveChangesAsync();

        return Ok(ToResponse(user));
    }

    [HttpPatch("{id:guid}/password")]
    public async Task<IActionResult> UpdatePassword(Guid id, [FromBody] UpdatePasswordRequest request)
    {
        if (!IsSelf(id))
        {
            return Forbid();
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound("User not found.");
        }

        var passwordValid = PasswordHasher.Verify(request.CurrentPassword, user.PasswordHash);
        if (!passwordValid)
        {
            return Unauthorized("Current password is invalid.");
        }

        user.PasswordHash = PasswordHasher.Hash(request.NewPassword);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound("User not found.");
        }

        dbContext.Users.Remove(user);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict("User cannot be deleted due to related records.");
        }

        return NoContent();
    }

    private bool IsSelfOrAdmin(Guid targetUserId)
    {
        return IsAdmin() || IsSelf(targetUserId);
    }

    private bool IsAdmin()
    {
        return User.IsInRole(nameof(UserRole.Admin));
    }

    private bool IsSelf(Guid targetUserId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var requesterId) && requesterId == targetUserId;
    }

    private static UserResponse ToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt
        };
    }
}

