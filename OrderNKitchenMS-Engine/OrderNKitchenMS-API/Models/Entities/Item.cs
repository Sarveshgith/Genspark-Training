using System.ComponentModel.DataAnnotations;
using OrderNKitchenMS_API.Models.Enums;

namespace OrderNKitchenMS_API.Models.Entities;

public class Item : BaseEntity
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public required string Name { get; set; }

    [Required]
    public ItemUnit Unit { get; set; }

    [Required, Range(0, 1000000)]
    public decimal StockQuantity { get; set; }

    [Required, Range(0, 1000000)]
    public decimal StockThreshold { get; set; }

    [Range(0, 1000000)]
    public decimal? CostPerUnit { get; set; }

    public bool IsActive { get; set; } = true;
}
