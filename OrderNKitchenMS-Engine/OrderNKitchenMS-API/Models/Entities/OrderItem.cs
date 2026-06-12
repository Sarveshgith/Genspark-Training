using System.ComponentModel.DataAnnotations;

namespace OrderNKitchenMS_API.Models.Entities;

public class OrderItem : BaseEntity
{
    public int Id {get; set;}

    [Required]
    public required int OrderId {get; set;}
    public Order Order {get; set;} = null!;

    [Required]
    public required int MenuItemId {get; set;}
    public MenuItem MenuItem {get; set;} = null!;

    [Required, Range(1, 1000)]
    public required int Quantity {get; set;}

    [Required, Range(0.01, 10000)]
    public decimal UnitPrice {get; set;}

    [MaxLength(500)]
    public string Notes {get; set;} = string.Empty;
}
