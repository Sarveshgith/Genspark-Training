using System;
using System.Collections.Generic;

namespace OrderNKitchenMS_API.Models.DTOs;

//In case of extending to phase 2, combine both table and user
public class OrderDto
{
	public int Id { get; set; }
	public int TableId { get; set; }
	public int TableNumber { get; set; }
	public int Status { get; set; }
	public string StatusName { get; set; } = string.Empty;
	public decimal TotalAmount { get; set; }
	public DateTime? CompletedAt { get; set; }
	public DateTime CreatedAt { get; set; }
	public int? AssignedChefId { get; set; }
	public string? AssignedChefName { get; set; }
	public int? AssignedWaiterId { get; set; }
	public string? AssignedWaiterName { get; set; }
	public DateTime? EstimatedReadyAt { get; set; }
	public IReadOnlyCollection<OrderItemDto> OrderItems { get; set; } = Array.Empty<OrderItemDto>();
}

public class OrderCreateDto
{
	public int TableId { get; set; }
	public List<OrderItemCreateDto> OrderItems { get; set; } = new();
}

public class QueryOrderDto
{
	public int? Status { get; set; }
	public int? TableId { get; set; }
	public DateTime? From { get; set; }
	public DateTime? To { get; set; }
	public int PageNumber { get; set; } = 1;
	public int PageSize { get; set; } = 10;
}

public class GuestOrderTrackingDto
{
	public int OrderId { get; set; }
	public int TableId { get; set; }
	public string Status { get; set; } = string.Empty;
	public int QueuePosition { get; set; }
	public DateTime? EstimatedReadyAt { get; set; }
	public int EstimatedTimeMinutes { get; set; }
	public IReadOnlyCollection<OrderItemTrackingDto> OrderItems { get; set; } = Array.Empty<OrderItemTrackingDto>();
}

public class OrderItemTrackingDto
{
	public string MenuItemName { get; set; } = string.Empty;
	public int Quantity { get; set; }
	public string Notes { get; set; } = string.Empty;
}
