using System.ComponentModel.DataAnnotations;
using OrderNKitchenMS_API.Models.Enums;
using System.Collections.Generic;

namespace OrderNKitchenMS_API.Models.Entities;

public class Bill : BaseEntity
{
    public int Id {get; set;}

    [Required]
    public required int OrderId {get; set;}
    public Order Order {get; set;} = null!;

    public decimal SubTotal {get; set;}

    public decimal TaxRate {get; set;}

    public decimal DiscountAmount {get; set;}

    public decimal TotalAmount {get; set;}

    public BillStatus Status {get; set;} = BillStatus.Pending;

    public ICollection<BillSplit> Splits { get; set; } = new List<BillSplit>();
}
