using System.Security.Claims;
using OrderNKitchenMS_API.Models.Entities;

namespace OrderNKitchenMS_API.Services.Interfaces;

public interface ITokenService
{
    bool VerifyPassword(string password, string storedHash);

    string HashPassword(string password);

    string CreateJwtToken(User user);

    string CreateRefreshJwtToken(User user);

    string CreateGuestToken(int tableId);

    ClaimsPrincipal ValidateRefreshToken(string token);
}
