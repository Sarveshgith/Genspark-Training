using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Utils;
using Microsoft.Extensions.Caching.Memory;

namespace OrderNKitchenMS_API.Services;

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;
    private readonly IMemoryCache _cache;

    public ReportService(IReportRepository reportRepository, IMemoryCache cache)
    {
        _reportRepository = reportRepository;
        _cache = cache;
    }

    public async Task<DailyRevenueDto> GetDailyRevenueReportAsync(DateTime date)
    {
        var cacheKey = CacheKeys.ReportDaily(date);
        DailyRevenueDto report;
        if (!_cache.TryGetValue(cacheKey, out report))
        {
            report = await _reportRepository.GetDailyRevenueAsync(date);
            _cache.Set(cacheKey, report, TimeSpan.FromMinutes(5));
        }
        return report;
    }

    public async Task<IEnumerable<RangeRevenueDto>> GetRangeRevenueReportAsync(DateTime fromDate, DateTime toDate)
    {
        Validation.Require(toDate >= fromDate, "From date cannot be after To date.", nameof(fromDate));
        var cacheKey = CacheKeys.ReportRange(fromDate, toDate);
        IEnumerable<RangeRevenueDto> report;
        if (!_cache.TryGetValue(cacheKey, out report))
        {
            report = await _reportRepository.GetRangeRevenueAsync(fromDate, toDate);
            _cache.Set(cacheKey, report, TimeSpan.FromMinutes(5));
        }
        return report;
    }

    public async Task<OrderSummaryDto> GetOrderSummaryReportAsync(DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate.HasValue && toDate.HasValue)
        {
            Validation.Require(toDate.Value >= fromDate.Value, "From date cannot be after To date.", nameof(fromDate));
        }
        var cacheKey = CacheKeys.ReportSummary(fromDate, toDate);
        OrderSummaryDto report;
        if (!_cache.TryGetValue(cacheKey, out report))
        {
            report = await _reportRepository.GetOrderSummaryAsync(fromDate, toDate);
            _cache.Set(cacheKey, report, TimeSpan.FromMinutes(5));
        }
        return report;
    }

    public async Task<IEnumerable<TopSellingItemDto>> GetTopSellingItemsReportAsync(int limit)
    {
        Validation.ValidateId(limit, nameof(limit), "Limit must be greater than zero.");
        var cacheKey = CacheKeys.ReportTopItems(limit);
        IEnumerable<TopSellingItemDto> report;
        if (!_cache.TryGetValue(cacheKey, out report))
        {
            report = await _reportRepository.GetTopSellingItemsAsync(limit);
            _cache.Set(cacheKey, report, TimeSpan.FromMinutes(5));
        }
        return report;
    }

    public async Task<IEnumerable<CategoryPerformanceDto>> GetCategoryPerformanceReportAsync(DateTime? fromDate, DateTime? toDate)
    {
        var cacheKey = CacheKeys.ReportCategoryPerformance(fromDate, toDate);
        IEnumerable<CategoryPerformanceDto> report;
        if (!_cache.TryGetValue(cacheKey, out report))
        {
            report = await _reportRepository.GetCategoryPerformanceAsync(fromDate, toDate);
            _cache.Set(cacheKey, report, TimeSpan.FromMinutes(5));
        }
        return report;
    }

    public async Task<KitchenSlaDto> GetKitchenSlaReportAsync(DateTime? fromDate, DateTime? toDate)
    {
        var cacheKey = CacheKeys.ReportKitchenSla(fromDate, toDate);
        KitchenSlaDto report;
        if (!_cache.TryGetValue(cacheKey, out report))
        {
            report = await _reportRepository.GetKitchenSlaAsync(fromDate, toDate);
            _cache.Set(cacheKey, report, TimeSpan.FromMinutes(5));
        }
        return report;
    }

    public async Task<IEnumerable<TableTurnoverDto>> GetTableTurnoverReportAsync(DateTime date)
    {
        var cacheKey = CacheKeys.ReportTableTurnover(date);
        IEnumerable<TableTurnoverDto> report;
        if (!_cache.TryGetValue(cacheKey, out report))
        {
            report = await _reportRepository.GetTableTurnoverAsync(date);
            _cache.Set(cacheKey, report, TimeSpan.FromMinutes(5));
        }
        return report;
    }
}
