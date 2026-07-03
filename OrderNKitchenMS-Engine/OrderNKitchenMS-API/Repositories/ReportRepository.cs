using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderNKitchenMS_API.Data;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Repositories.Interfaces;

namespace OrderNKitchenMS_API.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly AppDbContext _context;

    public ReportRepository(AppDbContext context)
    {
        _context = context;
    }

    private async Task<DbCommand> CreateCommandAsync(string query, Dictionary<string, object> parameters)
    {
        var connection = _context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        var command = connection.CreateCommand();
        command.CommandText = query;
        foreach (var param in parameters)
        {
            var dbParam = command.CreateParameter();
            dbParam.ParameterName = param.Key;
            dbParam.Value = param.Value ?? DBNull.Value;
            command.Parameters.Add(dbParam);
        }
        return command;
    }

    public async Task<DailyRevenueDto> GetDailyRevenueAsync(DateTime date)
    {
        const string query = @"
            SELECT ""TotalOrders"", ""TotalRevenue"", ""AvgOrderValue"", ""CancelledOrders""
            FROM fn_daily_revenue(@date::DATE);";

        using var command = await CreateCommandAsync(query, new() { { "@date", date.Date } });
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new DailyRevenueDto
            {
                TotalOrders = reader.GetInt32(0),
                TotalRevenue = reader.GetDecimal(1),
                AvgOrderValue = reader.GetDecimal(2),
                CancelledOrders = reader.GetInt32(3)
            };
        }
        return new DailyRevenueDto();
    }

    public async Task<IEnumerable<RangeRevenueDto>> GetRangeRevenueAsync(DateTime fromDate, DateTime toDate)
    {
        const string query = @"
            SELECT 
                d.date::DATE AS ""Date"",
                r.""TotalOrders"",
                r.""TotalRevenue"",
                r.""AvgOrderValue"",
                r.""CancelledOrders""
            FROM generate_series(@fromDate::DATE, @toDate::DATE, '1 day'::interval) d(date)
            CROSS JOIN LATERAL fn_daily_revenue(d.date::DATE) r;";

        var list = new List<RangeRevenueDto>();
        using var command = await CreateCommandAsync(query, new()
        {
            { "@fromDate", fromDate.Date },
            { "@toDate", toDate.Date }
        });
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new RangeRevenueDto
            {
                Date = reader.GetDateTime(0),
                TotalOrders = reader.GetInt32(1),
                TotalRevenue = reader.GetDecimal(2),
                AvgOrderValue = reader.GetDecimal(3),
                CancelledOrders = reader.GetInt32(4)
            });
        }
        return list;
    }

    public async Task<OrderSummaryDto> GetOrderSummaryAsync(DateTime? fromDate, DateTime? toDate)
    {
        const string query = @"
            SELECT ""TotalOrders"", ""CompletedOrders"", ""CancelledOrders"", ""CancellationRate""
            FROM fn_order_summary(@fromDate, @toDate);";

        using var command = await CreateCommandAsync(query, new()
        {
            { "@fromDate", (object?)fromDate?.Date ?? DBNull.Value },
            { "@toDate", (object?)toDate?.Date ?? DBNull.Value }
        });
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new OrderSummaryDto
            {
                TotalOrders = reader.GetInt32(0),
                CompletedOrders = reader.GetInt32(1),
                CancelledOrders = reader.GetInt32(2),
                CancellationRate = reader.GetDecimal(3)
            };
        }
        return new OrderSummaryDto();
    }

    public async Task<IEnumerable<TopSellingItemDto>> GetTopSellingItemsAsync(int limit)
    {
        const string query = @"
            SELECT ""ItemName"", ""Category"", ""TotalQtySold"", ""TotalRevenue"" 
            FROM vw_top_selling_items 
            ORDER BY ""TotalQtySold"" DESC, ""TotalRevenue"" DESC
            LIMIT @limit;";

        var list = new List<TopSellingItemDto>();
        using var command = await CreateCommandAsync(query, new() { { "@limit", limit } });
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new TopSellingItemDto
            {
                ItemName = reader.GetString(0),
                Category = reader.GetString(1),
                TotalQtySold = reader.GetInt32(2),
                TotalRevenue = reader.GetDecimal(3)
            });
        }
        return list;
    }

    public async Task<IEnumerable<CategoryPerformanceDto>> GetCategoryPerformanceAsync()
    {
        const string query = @"
            SELECT ""CategoryName"", ""OrderCount"", ""TotalRevenue""
            FROM vw_category_performance
            ORDER BY ""TotalRevenue"" DESC;";

        var list = new List<CategoryPerformanceDto>();
        using var command = await CreateCommandAsync(query, new());
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new CategoryPerformanceDto
            {
                CategoryName = reader.GetString(0),
                OrderCount = reader.GetInt32(1),
                TotalRevenue = reader.GetDecimal(2)
            });
        }
        return list;
    }

    public async Task<KitchenSlaDto> GetKitchenSlaAsync()
    {
        const string query = @"
            SELECT ""WithinSLA"", ""BreachedSLA"", ""SLAPercentage""
            FROM vw_kitchen_sla;";

        using var command = await CreateCommandAsync(query, new());
        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new KitchenSlaDto
            {
                WithinSLA = reader.GetInt32(0),
                BreachedSLA = reader.GetInt32(1),
                SLAPercentage = reader.GetDecimal(2)
            };
        }
        return new KitchenSlaDto();
    }

    public async Task<IEnumerable<TableTurnoverDto>> GetTableTurnoverAsync(DateTime date)
    {
        const string query = @"
            SELECT ""TableId"", ""TableNumber"", ""CompletedOrdersCount""
            FROM fn_table_turnover(@date::DATE)
            ORDER BY ""CompletedOrdersCount"" DESC, ""TableNumber"" ASC;";

        var list = new List<TableTurnoverDto>();
        using var command = await CreateCommandAsync(query, new() { { "@date", date.Date } });
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new TableTurnoverDto
            {
                TableId = reader.GetInt32(0),
                TableNumber = reader.GetInt32(1),
                CompletedOrdersCount = reader.GetInt32(2)
            });
        }
        return list;
    }
}
