using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using OrderNKitchenMS_API.Data;
using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Models.Enums;
using OrderNKitchenMS_API.Repositories;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Services;
using OrderNKitchenMS_API.Services.Interfaces;

namespace OrderNKitchenMS_API.Test.ServiceTests;

[TestFixture]
public class BillServiceTest
{
    private AppDbContext _context = null!;
    private IBillRepository _billRepository = null!;
    private Mock<IOrderService> _orderServiceMock = null!;
    private IBillService _billService = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "OrderNKitchenDb")
            .Options;

        _context = new AppDbContext(options);
        _billRepository = new BillRepository(_context);
        _orderServiceMock = new Mock<IOrderService>();

        var logger = new Mock<ILogger<BillService>>().Object;
        _billService = new BillService(_billRepository, _orderServiceMock.Object, logger);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task CreateBillAsync_PassTest()
    {
        // Arrange
        var orderDto = new OrderDto
        {
            Id = 1,
            Status = (int)OrderStatus.Served,
            TotalAmount = 100m,
            OrderItems = new []
            {
                new OrderItemDto { MenuItemId = 1, Quantity = 1, UnitPrice = 100m }
            }
        };
        _orderServiceMock.Setup(s => s.GetOrderByIdAsync(1)).ReturnsAsync(orderDto);

        var billCreateDto = new BillCreateDto
        {
            OrderId = 1,
            TaxRate = 10m,
            DiscountAmount = 5m
        };

        // Act
        var result = await _billService.CreateBillAsync(billCreateDto);

        // Assert
        Assert.That(result, Is.Not.Null);
        // TotalAmount = SubTotal (100) + Tax (10) - Discount (5) = 105
        Assert.That(result.TotalAmount, Is.EqualTo(105m));
        Assert.That(result.StatusName, Is.EqualTo("Pending"));
    }

    [Test]
    public void CreateBillAsync_FailTest_OrderNotReady_ThrowsBusinessRuleException()
    {
        // Arrange
        var orderDto = new OrderDto
        {
            Id = 1,
            Status = (int)OrderStatus.Pending, // Not Ready
            TotalAmount = 100m,
            OrderItems = new []
            {
                new OrderItemDto { MenuItemId = 1, Quantity = 1, UnitPrice = 100m }
            }
        };
        _orderServiceMock.Setup(s => s.GetOrderByIdAsync(1)).ReturnsAsync(orderDto);

        var billCreateDto = new BillCreateDto
        {
            OrderId = 1,
            TaxRate = 10m,
            DiscountAmount = 5m
        };

        // Act & Assert
        Assert.ThrowsAsync<BusinessRuleException>(async () => await _billService.CreateBillAsync(billCreateDto));
    }

    [Test]
    public async Task GetBillByOrderIdAsync_PassTest()
    {
        // Arrange
        var bill = new Bill
        {
            Id = 1,
            OrderId = 2,
            SubTotal = 50m,
            TaxRate = 5m,
            DiscountAmount = 2m,
            TotalAmount = 50.5m,
            Status = BillStatus.Pending
        };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        // Act
        var result = await _billService.GetBillByOrderIdAsync(2);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.OrderId, Is.EqualTo(2));
        Assert.That(result.TotalAmount, Is.EqualTo(50.5m));
    }

    [Test]
    public void GetBillByOrderIdAsync_FailTest_NotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _billService.GetBillByOrderIdAsync(999));
    }

    [Test]
    public async Task UpdateBillStatusAsync_PassTest()
    {
        // Arrange
        var bill = new Bill
        {
            Id = 1,
            OrderId = 3,
            SubTotal = 40m,
            TaxRate = 0m,
            DiscountAmount = 0m,
            TotalAmount = 40m,
            Status = BillStatus.Pending
        };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        _orderServiceMock.Setup(s => s.UpdateOrderStatusAsync(3, (int)OrderStatus.Completed)).ReturnsAsync(true);

        // Act: Transition Pending to Paid
        var success = await _billService.UpdateBillStatusAsync(1, "Paid");

        // Assert
        Assert.That(success, Is.True);
        var updatedBill = await _context.Bills.FindAsync(1);
        Assert.That(updatedBill!.Status, Is.EqualTo(BillStatus.Paid));
        _orderServiceMock.Verify(s => s.UpdateOrderStatusAsync(3, (int)OrderStatus.Completed), Times.Once);
    }

    [Test]
    public void UpdateBillStatusAsync_FailTest_AlreadyPaid_ThrowsBusinessRuleException()
    {
        // Arrange
        var bill = new Bill
        {
            Id = 1,
            OrderId = 3,
            SubTotal = 40m,
            TaxRate = 0m,
            DiscountAmount = 0m,
            TotalAmount = 40m,
            Status = BillStatus.Paid // Already Paid
        };
        _context.Bills.Add(bill);
        _context.SaveChanges();

        // Act & Assert
        Assert.ThrowsAsync<BusinessRuleException>(async () => await _billService.UpdateBillStatusAsync(1, "Failed"));
    }
}
