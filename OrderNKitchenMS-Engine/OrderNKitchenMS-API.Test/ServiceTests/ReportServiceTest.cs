using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Services;
using OrderNKitchenMS_API.Services.Interfaces;

namespace OrderNKitchenMS_API.Test.ServiceTests;

[TestFixture]
public class ReportServiceTest
{
    private Mock<IReportRepository> _repoMock = null!;
    private IMemoryCache _cache = null!;
    private IReportService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repoMock = new Mock<IReportRepository>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new ReportService(_repoMock.Object, _cache);
    }

    [TearDown]
    public void TearDown()
    {
        _cache.Dispose();
    }

    [Test]
    public async Task GetDailyRevenueReportAsync_Uncached_SetsCacheAndReturnsData()
    {
        // Arrange
        var date = DateTime.Today;
        var mockResult = new DailyRevenueDto { TotalRevenue = 1500, TotalOrders = 10 };
        _repoMock.Setup(r => r.GetDailyRevenueAsync(date)).ReturnsAsync(mockResult);

        // Act
        var result1 = await _service.GetDailyRevenueReportAsync(date);
        var result2 = await _service.GetDailyRevenueReportAsync(date);

        // Assert
        Assert.That(result1, Is.EqualTo(mockResult));
        Assert.That(result2, Is.EqualTo(mockResult));
        _repoMock.Verify(r => r.GetDailyRevenueAsync(date), Times.Once); // Verifies caching works
    }

    [Test]
    public async Task GetRangeRevenueReportAsync_Uncached_SetsCacheAndReturnsData()
    {
        // Arrange
        var fromDate = DateTime.Today.AddDays(-5);
        var toDate = DateTime.Today;
        var mockResult = new List<RangeRevenueDto> { new() { Date = DateTime.Today, TotalRevenue = 500 } };
        _repoMock.Setup(r => r.GetRangeRevenueAsync(fromDate, toDate)).ReturnsAsync(mockResult);

        // Act
        var result1 = await _service.GetRangeRevenueReportAsync(fromDate, toDate);
        var result2 = await _service.GetRangeRevenueReportAsync(fromDate, toDate);

        // Assert
        Assert.That(result1, Is.EqualTo(mockResult));
        Assert.That(result2, Is.EqualTo(mockResult));
        _repoMock.Verify(r => r.GetRangeRevenueAsync(fromDate, toDate), Times.Once);
    }

    [Test]
    public void GetRangeRevenueReportAsync_FromDateAfterToDate_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.GetRangeRevenueReportAsync(DateTime.Today, DateTime.Today.AddDays(-1)));
    }

    [Test]
    public async Task GetOrderSummaryReportAsync_Uncached_SetsCacheAndReturnsData()
    {
        // Arrange
        var fromDate = DateTime.Today.AddDays(-5);
        var toDate = DateTime.Today;
        var mockResult = new OrderSummaryDto { CompletedOrders = 20 };
        _repoMock.Setup(r => r.GetOrderSummaryAsync(fromDate, toDate)).ReturnsAsync(mockResult);

        // Act
        var result1 = await _service.GetOrderSummaryReportAsync(fromDate, toDate);
        var result2 = await _service.GetOrderSummaryReportAsync(fromDate, toDate);

        // Assert
        Assert.That(result1, Is.EqualTo(mockResult));
        Assert.That(result2, Is.EqualTo(mockResult));
        _repoMock.Verify(r => r.GetOrderSummaryAsync(fromDate, toDate), Times.Once);
    }

    [Test]
    public void GetOrderSummaryReportAsync_InvalidDateRange_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.GetOrderSummaryReportAsync(DateTime.Today, DateTime.Today.AddDays(-1)));
    }

    [Test]
    public async Task GetOrderSummaryReportAsync_SingleNullDates_Succeeds()
    {
        var mockResult = new OrderSummaryDto { CompletedOrders = 20 };
        _repoMock.Setup(r => r.GetOrderSummaryAsync(null, It.IsAny<DateTime?>())).ReturnsAsync(mockResult);
        _repoMock.Setup(r => r.GetOrderSummaryAsync(It.IsAny<DateTime?>(), null)).ReturnsAsync(mockResult);

        var res1 = await _service.GetOrderSummaryReportAsync(null, DateTime.Today);
        var res2 = await _service.GetOrderSummaryReportAsync(DateTime.Today, null);

        Assert.That(res1, Is.EqualTo(mockResult));
        Assert.That(res2, Is.EqualTo(mockResult));
    }

    [Test]
    public async Task GetTopSellingItemsReportAsync_Uncached_SetsCacheAndReturnsData()
    {
        // Arrange
        var mockResult = new List<TopSellingItemDto> { new() { ItemName = "Pizza", TotalQtySold = 45 } };
        _repoMock.Setup(r => r.GetTopSellingItemsAsync(5)).ReturnsAsync(mockResult);

        // Act
        var result1 = await _service.GetTopSellingItemsReportAsync(5);
        var result2 = await _service.GetTopSellingItemsReportAsync(5);

        // Assert
        Assert.That(result1, Is.EqualTo(mockResult));
        Assert.That(result2, Is.EqualTo(mockResult));
        _repoMock.Verify(r => r.GetTopSellingItemsAsync(5), Times.Once);
    }

    [Test]
    public void GetTopSellingItemsReportAsync_InvalidLimit_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(async () => await _service.GetTopSellingItemsReportAsync(0));
    }

    [Test]
    public async Task GetCategoryPerformanceReportAsync_Uncached_SetsCacheAndReturnsData()
    {
        // Arrange
        var mockResult = new List<CategoryPerformanceDto> { new() { CategoryName = "Desserts", TotalRevenue = 200 } };
        _repoMock.Setup(r => r.GetCategoryPerformanceAsync(null, null)).ReturnsAsync(mockResult);

        // Act
        var result1 = await _service.GetCategoryPerformanceReportAsync(null, null);
        var result2 = await _service.GetCategoryPerformanceReportAsync(null, null);

        // Assert
        Assert.That(result1, Is.EqualTo(mockResult));
        Assert.That(result2, Is.EqualTo(mockResult));
        _repoMock.Verify(r => r.GetCategoryPerformanceAsync(null, null), Times.Once);
    }

    [Test]
    public async Task GetKitchenSlaReportAsync_Uncached_SetsCacheAndReturnsData()
    {
        // Arrange
        var mockResult = new KitchenSlaDto { AvgPrepTimeMinutes = 2 };
        _repoMock.Setup(r => r.GetKitchenSlaAsync(null, null)).ReturnsAsync(mockResult);

        // Act
        var result1 = await _service.GetKitchenSlaReportAsync(null, null);
        var result2 = await _service.GetKitchenSlaReportAsync(null, null);

        // Assert
        Assert.That(result1, Is.EqualTo(mockResult));
        Assert.That(result2, Is.EqualTo(mockResult));
        _repoMock.Verify(r => r.GetKitchenSlaAsync(null, null), Times.Once);
    }

    [Test]
    public async Task GetTableTurnoverReportAsync_Uncached_SetsCacheAndReturnsData()
    {
        // Arrange
        var date = DateTime.Today;
        var mockResult = new List<TableTurnoverDto> { new() { TableId = 2, CompletedOrdersCount = 4 } };
        _repoMock.Setup(r => r.GetTableTurnoverAsync(date)).ReturnsAsync(mockResult);

        // Act
        var result1 = await _service.GetTableTurnoverReportAsync(date);
        var result2 = await _service.GetTableTurnoverReportAsync(date);

        // Assert
        Assert.That(result1, Is.EqualTo(mockResult));
        Assert.That(result2, Is.EqualTo(mockResult));
        _repoMock.Verify(r => r.GetTableTurnoverAsync(date), Times.Once);
    }
}
