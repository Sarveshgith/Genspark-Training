using System;

namespace OrderNKitchenMS_API.Models.DTOs;

public class BillDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class BillCreateDto
{
    public int OrderId { get; set; }
    public decimal TaxRate { get; set; }
    public decimal DiscountAmount { get; set; }
}
