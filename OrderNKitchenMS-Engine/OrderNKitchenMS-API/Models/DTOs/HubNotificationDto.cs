using System;

namespace OrderNKitchenMS_API.Models.DTOs;

public class HubNotificationDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Channel { get; set; } = string.Empty; // "Kitchen" | "Floor" | "Admin"
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Priority { get; set; } = "low"; // "low" | "medium" | "high"
    public string SenderName { get; set; } = string.Empty;
    public string SenderRole { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public int? TableId { get; set; }
    public string? ItemName { get; set; }
    public string? FlaggedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
    public object? Payload { get; set; }
}
