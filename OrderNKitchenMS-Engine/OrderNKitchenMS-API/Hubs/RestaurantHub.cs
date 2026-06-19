using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using OrderNKitchenMS_API.Utils;
using Microsoft.Extensions.Logging;

namespace OrderNKitchenMS_API.Hubs;

public class RestaurantHub : Hub
{
    private readonly ILogger<RestaurantHub> _logger;

    public RestaurantHub(ILogger<RestaurantHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("SignalR client connecting. ConnectionId: {ConnectionId}", Context.ConnectionId);

        if (Context.User != null && Context.User.Identity != null && Context.User.Identity.IsAuthenticated)
        {
            var sessionType = Context.User.FindFirst("SessionType")?.Value;
            _logger.LogInformation("Client authenticated. SessionType: {SessionType}, ConnectionId: {ConnectionId}", sessionType, Context.ConnectionId);

            if (sessionType == "Guest")
            {
                var tableId = Context.User.GetTableId();
                if (tableId.HasValue)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"table-{tableId.Value}");
                    _logger.LogInformation("Guest client assigned to group: table-{TableId} (ConnectionId: {ConnectionId})", tableId.Value, Context.ConnectionId);
                }
                else
                {
                    _logger.LogWarning("Guest client has no valid tableId claim. ConnectionId: {ConnectionId}", Context.ConnectionId);
                }
            }
            else
            {
                var role = Context.User.GetRole();
                _logger.LogInformation("Staff client connected. Role: {Role}, ConnectionId: {ConnectionId}", role, Context.ConnectionId);

                switch (role)
                {
                    case "Chef":
                        await Groups.AddToGroupAsync(Context.ConnectionId, "kitchen");
                        _logger.LogInformation("Chef assigned to group: kitchen (ConnectionId: {ConnectionId})", Context.ConnectionId);
                        break;

                    case "Waiter":
                        var waiterIdClaim = Context.User.GetUserId();
                        await Groups.AddToGroupAsync(Context.ConnectionId, $"waiter-{waiterIdClaim}");
                        _logger.LogInformation("Waiter assigned to group: waiter-{WaiterId} (ConnectionId: {ConnectionId})", waiterIdClaim, Context.ConnectionId);
                        break;

                    case "Admin":
                        await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
                        _logger.LogInformation("Admin assigned to group: admins (ConnectionId: {ConnectionId})", Context.ConnectionId);
                        break;

                    default:
                        _logger.LogWarning("Staff client has unrecognized role: {Role}. ConnectionId: {ConnectionId}", role, Context.ConnectionId);
                        break;
                }
            }
        }
        else
        {
            _logger.LogWarning("Unauthenticated client connected. ConnectionId: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }
}
