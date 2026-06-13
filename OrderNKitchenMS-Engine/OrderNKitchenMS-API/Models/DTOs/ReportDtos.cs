using System;

namespace OrderNKitchenMS_API.Models.DTOs;

public class DailyRevenueDto
{
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AvgOrderValue { get; set; }
    public int CancelledOrders { get; set; }
}

public class RangeRevenueDto
{
    public DateTime Date { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AvgOrderValue { get; set; }
    public int CancelledOrders { get; set; }
}

public class OrderSummaryDto
{
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal CancellationRate { get; set; }
}

public class TopSellingItemDto
{
    public string ItemName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int TotalQtySold { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class CategoryPerformanceDto
{
    public string CategoryName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class KitchenSlaDto
{
    public int WithinSLA { get; set; }
    public int BreachedSLA { get; set; }
    public decimal SLAPercentage { get; set; }
}

public class TableTurnoverDto
{
    public int TableId { get; set; }
    public int TableNumber { get; set; }
    public int CompletedOrdersCount { get; set; }
}
