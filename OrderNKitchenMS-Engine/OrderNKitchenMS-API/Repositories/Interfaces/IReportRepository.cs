using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrderNKitchenMS_API.Models.DTOs;

namespace OrderNKitchenMS_API.Repositories.Interfaces;

public interface IReportRepository
{
    Task<DailyRevenueDto> GetDailyRevenueAsync(DateTime date);
    Task<IEnumerable<RangeRevenueDto>> GetRangeRevenueAsync(DateTime fromDate, DateTime toDate);
    Task<OrderSummaryDto> GetOrderSummaryAsync(DateTime? fromDate, DateTime? toDate);
    Task<IEnumerable<TopSellingItemDto>> GetTopSellingItemsAsync(int limit);
    Task<IEnumerable<CategoryPerformanceDto>> GetCategoryPerformanceAsync();
    Task<KitchenSlaDto> GetKitchenSlaAsync();
    Task<IEnumerable<TableTurnoverDto>> GetTableTurnoverAsync(DateTime date);
}
