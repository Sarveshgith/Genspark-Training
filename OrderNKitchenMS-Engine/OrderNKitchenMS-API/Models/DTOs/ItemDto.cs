using System;
using OrderNKitchenMS_API.Models.Enums;

namespace OrderNKitchenMS_API.Models.DTOs;

public class ItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ItemUnit Unit { get; set; }
    public string UnitName => Unit.ToString();
    public decimal StockQuantity { get; set; }
    public decimal StockThreshold { get; set; }
    public decimal? CostPerUnit { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ItemCreateDto
{
    public string Name { get; set; } = string.Empty;
    public ItemUnit Unit { get; set; }
    public decimal StockQuantity { get; set; }
    public decimal StockThreshold { get; set; }
    public decimal? CostPerUnit { get; set; }
}

public class ItemUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public ItemUnit Unit { get; set; }
    public decimal StockQuantity { get; set; }
    public decimal? StockThreshold { get; set; }
    public decimal? CostPerUnit { get; set; }
    public bool IsActive { get; set; }
}

public class MenuItemIngredientDto
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal QuantityRequired { get; set; }
}

public class MenuItemIngredientCreateDto
{
    public int ItemId { get; set; }
    public decimal QuantityRequired { get; set; }
}
