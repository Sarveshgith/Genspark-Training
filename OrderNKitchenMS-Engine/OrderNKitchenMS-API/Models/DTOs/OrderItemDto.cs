using System;

namespace OrderNKitchenMS_API.Models.DTOs;

public class OrderItemDto
{
	public int Id { get; set; }
	public int OrderId { get; set; }
	public int MenuItemId { get; set; }
	public string MenuItemName { get; set; } = string.Empty;
	public int Quantity { get; set; }
	public decimal UnitPrice { get; set; }
	public string Notes { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
}

public class OrderItemCreateDto
{
	public int MenuItemId { get; set; }
	public int Quantity { get; set; }
	public string Notes { get; set; } = string.Empty;
}
