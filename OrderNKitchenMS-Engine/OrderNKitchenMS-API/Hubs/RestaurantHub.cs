using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using OrderNKitchenMS_API.Utils;
using Microsoft.Extensions.Logging;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Models.DTOs;

namespace OrderNKitchenMS_API.Hubs;

public class RestaurantHub : Hub
{
    private readonly ILogger<RestaurantHub> _logger;
    private readonly ISignalService _signalService;

    public RestaurantHub(ILogger<RestaurantHub> logger, ISignalService signalService)
    {
        _logger = logger;
        _signalService = signalService;
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
                        await Groups.AddToGroupAsync(Context.ConnectionId, "waiters");
                        _logger.LogInformation("Waiter assigned to groups: waiter-{WaiterId} and waiters (ConnectionId: {ConnectionId})", waiterIdClaim, Context.ConnectionId);
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

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> _cooldowns = new();

    public async Task SendKitchenMessage(string type, System.Text.Json.JsonElement payload)
    {
        if (Context.User == null || !Context.User.Identity.IsAuthenticated)
        {
            throw new HubException("Unauthenticated.");
        }

        var role = Context.User.GetRole();
        if (role != "Waiter" && role != "Admin")
        {
            throw new HubException("Unauthorized to send kitchen messages.");
        }

        var allowedTypes = new[] { "order_reminder", "priority_flag", "special_instruction_update", "cancel_warning" };
        if (Array.IndexOf(allowedTypes, type) < 0)
        {
            throw new HubException($"Invalid notification type: {type}");
        }

        int? orderId = null;
        int? tableId = null;
        string? note = null;

        if (payload.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            if (payload.TryGetProperty("orderId", out var orderIdProp) && orderIdProp.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                orderId = orderIdProp.GetInt32();
            }
            if (payload.TryGetProperty("tableId", out var tableIdProp) && tableIdProp.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                tableId = tableIdProp.GetInt32();
            }
            if (payload.TryGetProperty("note", out var noteProp) && noteProp.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                note = noteProp.GetString();
            }
        }

        if (type == "order_reminder" && orderId.HasValue)
        {
            var cooldownKey = $"order-reminder-{orderId.Value}";
            if (_cooldowns.TryGetValue(cooldownKey, out var lastSent))
            {
                if (DateTime.UtcNow - lastSent < TimeSpan.FromMinutes(5))
                {
                    throw new HubException("Order reminder is on cooldown. Please wait 5 minutes.");
                }
            }
            _cooldowns[cooldownKey] = DateTime.UtcNow;
        }

        var title = type switch
        {
            "order_reminder" => "Reminder",
            "priority_flag" => "Priority Flag",
            "special_instruction_update" => "Special Instruction Update",
            "cancel_warning" => "Cancellation Warning",
            _ => "Kitchen Alert"
        };

        var message = type switch
        {
            "order_reminder" => "Customer waiting longer than expected.",
            "priority_flag" => "High priority preparation request.",
            "special_instruction_update" => string.IsNullOrEmpty(note) ? "Special instructions have been updated." : note,
            "cancel_warning" => "Warning: Order cancellation request.",
            _ => ""
        };

        var priority = type switch
        {
            "priority_flag" => "high",
            "cancel_warning" => "high",
            _ => "medium"
        };

        var senderName = Context.User.Identity.Name ?? role;

        var notification = new HubNotificationDto
        {
            Channel = "Kitchen",
            Type = type,
            Title = title,
            Message = message,
            Priority = priority,
            SenderName = senderName,
            SenderRole = role,
            OrderId = orderId,
            TableId = tableId,
            Payload = payload
        };

        await _signalService.SendKitchenMessageAsync(notification);
    }

    public async Task SendFloorMessage(string type, int tableId, System.Text.Json.JsonElement payload)
    {
        if (Context.User == null || !Context.User.Identity.IsAuthenticated)
        {
            throw new HubException("Unauthenticated.");
        }

        var role = Context.User.GetRole();
        if (role != "Chef" && role != "Admin")
        {
            throw new HubException("Unauthorized to send floor messages.");
        }

        var allowedTypes = new[] { "order_ready_reminder", "order_delayed", "item_substitution_needed", "low_stock_warning_floor" };
        if (Array.IndexOf(allowedTypes, type) < 0)
        {
            throw new HubException($"Invalid notification type: {type}");
        }

        int? orderId = null;
        string? substituteItem = null;
        string? note = null;

        if (payload.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            if (payload.TryGetProperty("orderId", out var orderIdProp) && orderIdProp.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                orderId = orderIdProp.GetInt32();
            }
            if (payload.TryGetProperty("substituteItem", out var subProp) && subProp.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                substituteItem = subProp.GetString();
            }
            if (payload.TryGetProperty("note", out var noteProp) && noteProp.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                note = noteProp.GetString();
            }
        }

        var title = type switch
        {
            "order_ready_reminder" => "Order Ready",
            "order_delayed" => "Order Delayed",
            "item_substitution_needed" => "Item Substitution",
            "low_stock_warning_floor" => "Low Stock Warning",
            _ => "Floor Alert"
        };

        var message = type switch
        {
            "order_ready_reminder" => $"Order #{orderId} is ready to serve.",
            "order_delayed" => string.IsNullOrEmpty(note) ? $"Order #{orderId} is delayed." : note,
            "item_substitution_needed" => string.IsNullOrEmpty(substituteItem) ? "Substitution required." : $"{note} Offer {substituteItem} instead.",
            "low_stock_warning_floor" => "An ingredient is running low on stock.",
            _ => ""
        };

        var priority = "medium";
        var senderName = Context.User.Identity.Name ?? role;

        var notification = new HubNotificationDto
        {
            Channel = "Floor",
            Type = type,
            Title = title,
            Message = message,
            Priority = priority,
            SenderName = senderName,
            SenderRole = role,
            OrderId = orderId,
            TableId = tableId,
            Payload = payload
        };

        await _signalService.SendFloorMessageAsync(notification);
    }

    public async Task SendAdminAlert(string type, System.Text.Json.JsonElement payload)
    {
        if (Context.User == null || !Context.User.Identity.IsAuthenticated)
        {
            throw new HubException("Unauthenticated.");
        }

        var role = Context.User.GetRole();
        if (role != "Chef" && role != "Waiter")
        {
            throw new HubException("Unauthorized to send admin alerts.");
        }

        var allowedTypes = new[] { "low_stock_critical", "equipment_issue", "staff_request", "order_dispute" };
        if (Array.IndexOf(allowedTypes, type) < 0)
        {
            throw new HubException($"Invalid notification type: {type}");
        }

        int? itemId = null;
        decimal? currentQuantity = null;
        string? itemName = null;
        string? note = null;

        if (payload.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            if (payload.TryGetProperty("itemId", out var itemIdProp) && itemIdProp.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                itemId = itemIdProp.GetInt32();
            }
            if (payload.TryGetProperty("currentQuantity", out var qtyProp) && qtyProp.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                currentQuantity = qtyProp.GetDecimal();
            }
            if (payload.TryGetProperty("itemName", out var nameProp) && nameProp.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                itemName = nameProp.GetString();
            }
            if (payload.TryGetProperty("note", out var noteProp) && noteProp.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                note = noteProp.GetString();
            }
        }

        if (type == "low_stock_critical" && itemId.HasValue)
        {
            var cooldownKey = $"stock-alert-{itemId.Value}";
            if (_cooldowns.TryGetValue(cooldownKey, out var lastSent))
            {
                if (DateTime.UtcNow - lastSent < TimeSpan.FromMinutes(5))
                {
                    throw new HubException("Low stock alert is on cooldown. Please wait 5 minutes.");
                }
            }
            _cooldowns[cooldownKey] = DateTime.UtcNow;
        }

        var title = type switch
        {
            "low_stock_critical" => "Low Stock",
            "equipment_issue" => "Equipment Issue",
            "staff_request" => "Staff Request",
            "order_dispute" => "Order Dispute",
            _ => "Admin Alert"
        };

        var message = type switch
        {
            "low_stock_critical" => $"Ingredient low stock warning: {itemName} ({currentQuantity} units remaining).",
            "equipment_issue" => string.IsNullOrEmpty(note) ? "An equipment issue was reported." : note,
            "staff_request" => "A staff request has been issued.",
            "order_dispute" => string.IsNullOrEmpty(note) ? "An order dispute was reported." : note,
            _ => ""
        };

        var priority = type switch
        {
            "low_stock_critical" => "high",
            "equipment_issue" => "high",
            "order_dispute" => "high",
            _ => "medium"
        };

        var senderName = Context.User.Identity.Name ?? role;

        var notification = new HubNotificationDto
        {
            Channel = "Admin",
            Type = type,
            Title = title,
            Message = message,
            Priority = priority,
            SenderName = senderName,
            SenderRole = role,
            ItemName = itemName,
            FlaggedBy = senderName,
            Payload = payload
        };

        await _signalService.SendAdminAlertAsync(notification);
    }

    public async Task GuestCallWaiter(string type)
    {
        if (Context.User == null || Context.User.Identity == null || !Context.User.Identity.IsAuthenticated)
        {
            throw new HubException("Unauthenticated.");
        }

        var sessionType = Context.User.FindFirst("SessionType")?.Value;
        if (sessionType != "Guest")
        {
            throw new HubException("Only guests can call the waiter.");
        }

        var tableId = Context.User.GetTableId();
        if (!tableId.HasValue)
        {
            throw new HubException("Guest has no associated table ID.");
        }

        var allowedTypes = new[] { "assistance", "ordering", "bill" };
        if (Array.IndexOf(allowedTypes, type) < 0)
        {
            throw new HubException($"Invalid call type: {type}");
        }

        var title = type switch
        {
            "assistance" => "General Assistance Needed",
            "ordering" => "Ready to Order",
            "bill" => "Bill Requested",
            _ => "Waiter Requested"
        };

        var message = type switch
        {
            "assistance" => $"Table #{tableId.Value} is requesting general assistance.",
            "ordering" => $"Table #{tableId.Value} is ready to place an order or needs help ordering.",
            "bill" => $"Table #{tableId.Value} has requested their final bill.",
            _ => $"Table #{tableId.Value} is calling for a waiter."
        };

        var notification = new HubNotificationDto
        {
            Channel = "Floor",
            Type = $"guest_call_{type}",
            Title = title,
            Message = message,
            Priority = "high",
            SenderName = $"Table {tableId.Value}",
            SenderRole = "Guest",
            TableId = tableId.Value,
            CreatedAt = DateTime.UtcNow
        };

        await _signalService.SendFloorMessageAsync(notification);
        _logger.LogInformation("Guest at Table {TableId} triggered waiter call of type: {CallType}", tableId.Value, type);
    }
}

