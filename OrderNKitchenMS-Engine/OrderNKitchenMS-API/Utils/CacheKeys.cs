using System;

namespace OrderNKitchenMS_API.Utils;

public static class CacheKeys
{
    public const string MenuAll = "menu:all";
    public const string CategoriesAll = "categories:all";
    public const string RolesAll = "roles:all";

    public static string ReportDaily(DateTime date) => $"reports:daily:{date:yyyy-MM-dd}";
    public static string ReportRange(DateTime from, DateTime to) => $"reports:range:{from:yyyy-MM-dd}:{to:yyyy-MM-dd}";
    public static string ReportSummary(DateTime? from, DateTime? to) => $"reports:summary:{from?.ToString("yyyy-MM-dd") ?? "null"}:{to?.ToString("yyyy-MM-dd") ?? "null"}";
    public static string ReportTopItems(int limit) => $"reports:top-items:{limit}";
    public static string ReportCategoryPerformance(DateTime? from, DateTime? to) => $"reports:category-perf:{from?.ToString("yyyy-MM-dd") ?? "null"}:{to?.ToString("yyyy-MM-dd") ?? "null"}";
    public static string ReportKitchenSla(DateTime? from, DateTime? to) => $"reports:kitchen-sla:{from?.ToString("yyyy-MM-dd") ?? "null"}:{to?.ToString("yyyy-MM-dd") ?? "null"}";
    public static string ReportTableTurnover(DateTime date) => $"reports:table-turnover:{date:yyyy-MM-dd}";
}
