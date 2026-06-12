using System.Security.Claims;
using OrderNKitchenMS_API.Exceptions;

namespace OrderNKitchenMS_API.Utils;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        if (principal == null) throw new ArgumentNullException(nameof(principal));

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedException("User ID claim is missing or invalid.");
        }
        return userId;
    }

    public static int? GetTableId(this ClaimsPrincipal principal)
    {
        if (principal == null) throw new ArgumentNullException(nameof(principal));

        var tableIdClaim = principal.FindFirst("tableId")?.Value;
        if (string.IsNullOrEmpty(tableIdClaim) || !int.TryParse(tableIdClaim, out var tableId))
        {
            return null;
        }
        return tableId;
    }

    public static string GetRole(this ClaimsPrincipal principal)
    {
        if (principal == null) throw new ArgumentNullException(nameof(principal));

        var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;
        return roleClaim ?? string.Empty;
    }
}
