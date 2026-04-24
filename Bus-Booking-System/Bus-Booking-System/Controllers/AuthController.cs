using Bus_Booking_System.Data;
using Bus_Booking_System.Models.DTOs;
using Bus_Booking_System.Models.Entities;
using Bus_Booking_System.Models.Enums;
using Bus_Booking_System.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Bus_Booking_System.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(AppDbContext dbContext, IConfiguration configuration) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var phone = request.Phone.Trim();

        if (request.Role == UserRole.Admin)
        {
            return BadRequest("Use /auth/admin/register for admin creation.");
        }

        var emailExists = await dbContext.Users.AnyAsync(x => x.Email == email);
        if (emailExists)
        {
            return Conflict("Email already exists.");
        }

        var phoneExists = await dbContext.Users.AnyAsync(x => x.Phone == phone);
        if (phoneExists)
        {
            return Conflict("Phone already exists.");
        }

        if (request.Role == UserRole.Operator && string.IsNullOrWhiteSpace(request.LicenseNumber))
        {
            return BadRequest("License number is required for operator registration.");
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            Phone = phone,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = request.Role
        };

        dbContext.Users.Add(user);

        if (request.Role == UserRole.Operator)
        {
            dbContext.Operators.Add(new Operator
            {
                UserId = user.Id,
                LicenseNumber = request.LicenseNumber!.Trim(),
                Status = OperatorStatus.Pending
            });
        }

        await dbContext.SaveChangesAsync();

        return Ok(BuildAuthResponse(user));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user is null)
        {
            return Unauthorized("Invalid credentials.");
        }

        var passwordValid = PasswordHasher.Verify(request.Password, user.PasswordHash);
        if (!passwordValid)
        {
            return Unauthorized("Invalid credentials.");
        }

        return Ok(BuildAuthResponse(user));
    }

    [HttpPost("admin/register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> RegisterAdmin([FromBody] AdminRegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var phone = request.Phone.Trim();

        var emailExists = await dbContext.Users.AnyAsync(x => x.Email == email);
        if (emailExists)
        {
            return Conflict("Email already exists.");
        }

        var phoneExists = await dbContext.Users.AnyAsync(x => x.Phone == phone);
        if (phoneExists)
        {
            return Conflict("Phone already exists.");
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            Phone = phone,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = UserRole.Admin
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        return Ok(BuildAuthResponse(user));
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var expiryMinutes = configuration.GetValue<int>("Jwt:ExpiryMinutes");
        if (expiryMinutes <= 0)
        {
            expiryMinutes = 120;
        }

        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is missing in configuration.");
        var audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is missing in configuration.");
        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is missing in configuration.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AuthResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresInSeconds = expiryMinutes * 60,
            UserId = user.Id,
            Role = user.Role.ToString(),
            Email = user.Email
        };
    }
}
