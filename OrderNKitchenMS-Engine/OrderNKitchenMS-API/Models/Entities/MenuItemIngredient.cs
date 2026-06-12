using System.ComponentModel.DataAnnotations;

namespace OrderNKitchenMS_API.Models.Entities;

public class MenuItemIngredient : BaseEntity
{
    public int Id { get; set; }

    [Required]
    public int MenuItemId { get; set; }
    public MenuItem MenuItem { get; set; } = null!;

    [Required]
    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;

    [Required, Range(0.0001, 1000000)]
    public decimal QuantityRequired { get; set; }
}
