using System.ComponentModel.DataAnnotations;
using OrderNKitchenMS_API.Models.Enums;

namespace OrderNKitchenMS_API.Models.Entities;

public class Order : BaseEntity
{
    public int Id {get; set;}

    [Required]
    public required int TableId {get; set;}
    public Table Table {get; set;} = null!;
    
    public int? AssignedUserId {get; set;}
    public User AssignedUser {get; set;} = null!;

    [Required]
    public OrderStatus Status {get; set;} = OrderStatus.Pending;

    public decimal TotalAmount {get; set;}

    public DateTime? CompletedAt {get; set;}
}
