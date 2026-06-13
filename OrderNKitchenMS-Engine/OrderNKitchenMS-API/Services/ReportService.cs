using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Services.Interfaces;

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
        if (fromDate > toDate)
        {
            throw new BusinessRuleException("From date cannot be after To date.");
        }
        return await _reportRepository.GetRangeRevenueAsync(fromDate, toDate);
    }

    public async Task<OrderSummaryDto> GetOrderSummaryReportAsync(DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
        {
            throw new BusinessRuleException("From date cannot be after To date.");
        }
        return await _reportRepository.GetOrderSummaryAsync(fromDate, toDate);
    }

    public async Task<IEnumerable<TopSellingItemDto>> GetTopSellingItemsReportAsync(int limit)
    {
        if (limit <= 0)
        {
            throw new BusinessRuleException("Limit must be greater than zero.");
        }
        return await _reportRepository.GetTopSellingItemsAsync(limit);
    }

    public async Task<IEnumerable<CategoryPerformanceDto>> GetCategoryPerformanceReportAsync()
    {
        return await _reportRepository.GetCategoryPerformanceAsync();
    }

    public async Task<KitchenSlaDto> GetKitchenSlaReportAsync()
    {
        return await _reportRepository.GetKitchenSlaAsync();
    }

    public async Task<IEnumerable<TableTurnoverDto>> GetTableTurnoverReportAsync(DateTime date)
    {
        return await _reportRepository.GetTableTurnoverAsync(date);
    }
}
