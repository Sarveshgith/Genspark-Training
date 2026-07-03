using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Exceptions;

namespace OrderNKitchenMS_API.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    // Verifies if the password matches the stored hash.
    public bool VerifyPassword(string password, string storedHash)
    {
        var result = _passwordHasher.VerifyHashedPassword(null!, storedHash, password);
        return result == PasswordVerificationResult.Success;
    }

    // Computes a hashed password.
    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(null!, password);
    }

    // Creates a JWT access token for a user.
    public string CreateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured.");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role?.Name.ToString() ?? string.Empty),
            new Claim("SessionType", "User")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresInStr = jwtSettings["ExpiresInMinutes"];
        double expiresInMinutes = 60;
        if (!string.IsNullOrEmpty(expiresInStr) && double.TryParse(expiresInStr, out var parsedExpires))
        {
            expiresInMinutes = parsedExpires;
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Creates a JWT refresh token for a user.
    public string CreateRefreshJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretRefreshKey"] ?? throw new InvalidOperationException("JWT RefreshSecretKey is not configured.");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured.");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role?.Name.ToString() ?? string.Empty),
            new Claim("SessionType", "User")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var refreshExpiresInStr = jwtSettings["RefreshExpiresInDays"];
        double refreshExpiresInDays = 7;
        if (!string.IsNullOrEmpty(refreshExpiresInStr) && double.TryParse(refreshExpiresInStr, out var parsedRefreshExpires))
        {
            refreshExpiresInDays = parsedRefreshExpires;
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(refreshExpiresInDays),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Creates a guest JWT token for a specific table ID.
    public string CreateGuestToken(int tableId)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured.");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");

        var claims = new List<Claim>
        {
            new Claim("tableId", tableId.ToString()),
            new Claim("SessionType", "Guest")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var guestExpiresInStr = jwtSettings["GuestExpiresInHours"];
        double guestExpiresInHours = 3;
        if (!string.IsNullOrEmpty(guestExpiresInStr) && double.TryParse(guestExpiresInStr, out var parsedGuestExpires))
        {
            guestExpiresInHours = parsedGuestExpires;
        }

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(guestExpiresInHours),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // Validates a JWT refresh token and returns the corresponding ClaimsPrincipal.
    public ClaimsPrincipal ValidateRefreshToken(string token)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretRefreshKey"] ?? throw new InvalidOperationException("JWT RefreshSecretKey is not configured.");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured.");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var tokenHandler = new JwtSecurityTokenHandler();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token algorithm.");
            }

            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token validation failed.");
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }
    }
}
