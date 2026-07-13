using System.ComponentModel.DataAnnotations;
using OrderNKitchenMS_API.Models.Enums;

namespace OrderNKitchenMS_API.Models.Entities;

public class BillSplit : BaseEntity
{
    public int Id { get; set; }

    [Required]
    public int BillId { get; set; }
    public Bill Bill { get; set; } = null!;

    public decimal Amount { get; set; }

    public BillStatus Status { get; set; } = BillStatus.Pending;
}
