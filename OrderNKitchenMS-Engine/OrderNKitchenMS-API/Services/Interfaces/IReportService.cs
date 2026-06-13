using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrderNKitchenMS_API.Models.DTOs;

namespace OrderNKitchenMS_API.Services.Interfaces;

public interface IReportService
{
    Task<DailyRevenueDto> GetDailyRevenueReportAsync(DateTime date);
    Task<IEnumerable<RangeRevenueDto>> GetRangeRevenueReportAsync(DateTime fromDate, DateTime toDate);
    Task<OrderSummaryDto> GetOrderSummaryReportAsync(DateTime? fromDate, DateTime? toDate);
    Task<IEnumerable<TopSellingItemDto>> GetTopSellingItemsReportAsync(int limit);
    Task<IEnumerable<CategoryPerformanceDto>> GetCategoryPerformanceReportAsync();
    Task<KitchenSlaDto> GetKitchenSlaReportAsync();
    Task<IEnumerable<TableTurnoverDto>> GetTableTurnoverReportAsync(DateTime date);
}
