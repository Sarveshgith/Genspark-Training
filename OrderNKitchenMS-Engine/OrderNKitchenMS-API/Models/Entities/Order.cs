using System.ComponentModel.DataAnnotations;
using OrderNKitchenMS_API.Models.Enums;

namespace OrderNKitchenMS_API.Models.Entities;

public class Order : BaseEntity
{
    public int Id {get; set;}

    [Required]
    public required int TableId {get; set;}
    public Table Table {get; set;} = null!;
    
    public int? AssignedChefId {get; set;}
    public User? AssignedChef {get; set;}
    
    public int? AssignedWaiterId {get; set;}
    public User? AssignedWaiter {get; set;}

    [Required]
    public OrderStatus Status {get; set;} = OrderStatus.Pending;

    public decimal TotalAmount {get; set;}

    public DateTime? CompletedAt {get; set;}
}
