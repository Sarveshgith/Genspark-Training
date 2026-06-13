using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Services.Interfaces;

namespace OrderNKitchenMS_API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = "AdminOnly")]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(IReportService reportService, ILogger<ReportController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [HttpGet("revenue/daily")]
    public async Task<ActionResult<DailyRevenueDto>> GetDailyRevenue([FromQuery] DateTime? date)
    {
        var targetDate = date ?? DateTime.UtcNow.Date;
        _logger.LogInformation("GetDailyRevenue requested for date: {Date}", targetDate);
        var result = await _reportService.GetDailyRevenueReportAsync(targetDate);
        return Ok(result);
    }

    [HttpGet("revenue/range")]
    public async Task<ActionResult<IEnumerable<RangeRevenueDto>>> GetRangeRevenue([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        _logger.LogInformation("GetRangeRevenue requested from {From} to {To}", from, to);
        var result = await _reportService.GetRangeRevenueReportAsync(from, to);
        return Ok(result);
    }

    [HttpGet("orders/summary")]
    public async Task<ActionResult<OrderSummaryDto>> GetOrderSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        _logger.LogInformation("GetOrderSummary requested from {From} to {To}", from, to);
        var result = await _reportService.GetOrderSummaryReportAsync(from, to);
        return Ok(result);
    }

    [HttpGet("menu/top-items")]
    public async Task<ActionResult<IEnumerable<TopSellingItemDto>>> GetTopSellingItems([FromQuery] int limit = 5)
    {
        _logger.LogInformation("GetTopSellingItems requested with limit: {Limit}", limit);
        var result = await _reportService.GetTopSellingItemsReportAsync(limit);
        return Ok(result);
    }

    [HttpGet("menu/category-performance")]
    public async Task<ActionResult<IEnumerable<CategoryPerformanceDto>>> GetCategoryPerformance()
    {
        _logger.LogInformation("GetCategoryPerformance requested");
        var result = await _reportService.GetCategoryPerformanceReportAsync();
        return Ok(result);
    }

    [HttpGet("kitchen/sla")]
    public async Task<ActionResult<KitchenSlaDto>> GetKitchenSla()
    {
        _logger.LogInformation("GetKitchenSla requested");
        var result = await _reportService.GetKitchenSlaReportAsync();
        return Ok(result);
    }

    [HttpGet("tables/turnover")]
    public async Task<ActionResult<IEnumerable<TableTurnoverDto>>> GetTableTurnover([FromQuery] DateTime? date)
    {
        var targetDate = date ?? DateTime.UtcNow.Date;
        _logger.LogInformation("GetTableTurnover requested for date: {Date}", targetDate);
        var result = await _reportService.GetTableTurnoverReportAsync(targetDate);
        return Ok(result);
    }
}
