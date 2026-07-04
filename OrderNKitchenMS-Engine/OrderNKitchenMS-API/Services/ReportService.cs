using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Utils;

namespace OrderNKitchenMS_API.Services;

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;

    public ReportService(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<DailyRevenueDto> GetDailyRevenueReportAsync(DateTime date)
    {
        return await _reportRepository.GetDailyRevenueAsync(date);
    }

    public async Task<IEnumerable<RangeRevenueDto>> GetRangeRevenueReportAsync(DateTime fromDate, DateTime toDate)
    {
        Validation.Require(toDate >= fromDate, "From date cannot be after To date.", nameof(fromDate));
        return await _reportRepository.GetRangeRevenueAsync(fromDate, toDate);
    }

    public async Task<OrderSummaryDto> GetOrderSummaryReportAsync(DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue)
        {
            Validation.Require(toDate.Value >= fromDate.Value, "From date cannot be after To date.", nameof(fromDate));
        }
        return await _reportRepository.GetOrderSummaryAsync(fromDate, toDate);
    }

    public async Task<IEnumerable<TopSellingItemDto>> GetTopSellingItemsReportAsync(int limit)
    {
        Validation.ValidateId(limit, nameof(limit), "Limit must be greater than zero.");
        return await _reportRepository.GetTopSellingItemsAsync(limit);
    }

    public async Task<IEnumerable<CategoryPerformanceDto>> GetCategoryPerformanceReportAsync(DateTime? fromDate, DateTime? toDate)
    {
        return await _reportRepository.GetCategoryPerformanceAsync(fromDate, toDate);
    }

    public async Task<KitchenSlaDto> GetKitchenSlaReportAsync(DateTime? fromDate, DateTime? toDate)
    {
        return await _reportRepository.GetKitchenSlaAsync(fromDate, toDate);
    }

    public async Task<IEnumerable<TableTurnoverDto>> GetTableTurnoverReportAsync(DateTime date)
    {
        return await _reportRepository.GetTableTurnoverAsync(date);
    }
}
