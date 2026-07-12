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
    private Mock<ISignalService> _signalServiceMock = null!;
    private IBillService _billService = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _billRepository = new BillRepository(_context);
        _orderServiceMock = new Mock<IOrderService>();
        _signalServiceMock = new Mock<ISignalService>();

        var logger = new Mock<ILogger<BillService>>().Object;
        _billService = new BillService(_billRepository, _orderServiceMock.Object, _signalServiceMock.Object, logger);
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
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
        _orderServiceMock.Setup(s => s.GetOrderByIdAsync(3)).ReturnsAsync(new OrderDto { Id = 3, TableId = 2 });

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

    [Test]
    public void CreateBillAsync_OrderNotFound_ThrowsNotFoundException()
    {
        _orderServiceMock.Setup(s => s.GetOrderByIdAsync(1)).ReturnsAsync((OrderDto?)null);
        var dto = new BillCreateDto { OrderId = 1, TaxRate = 5, DiscountAmount = 0 };
        Assert.ThrowsAsync<NotFoundException>(async () => await _billService.CreateBillAsync(dto));
    }

    [Test]
    public async Task CreateBillAsync_ExistingBill_ThrowsBusinessRuleException()
    {
        var bill = new Bill { Id = 10, OrderId = 1, SubTotal = 50, Status = BillStatus.Pending };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        var orderDto = new OrderDto { Id = 1, Status = (int)OrderStatus.Served, TotalAmount = 50 };
        _orderServiceMock.Setup(s => s.GetOrderByIdAsync(1)).ReturnsAsync(orderDto);

        var dto = new BillCreateDto { OrderId = 1, TaxRate = 5, DiscountAmount = 0 };
        Assert.ThrowsAsync<BusinessRuleException>(async () => await _billService.CreateBillAsync(dto));
    }

    [Test]
    public void CreateBillAsync_InvalidTaxRateNegative_ThrowsBusinessRuleException()
    {
        var orderDto = new OrderDto { Id = 1, Status = (int)OrderStatus.Served, TotalAmount = 50, OrderItems = new [] { new OrderItemDto { MenuItemId = 1, Quantity = 1, UnitPrice = 50 } } };
        _orderServiceMock.Setup(s => s.GetOrderByIdAsync(1)).ReturnsAsync(orderDto);

        var dto = new BillCreateDto { OrderId = 1, TaxRate = -1, DiscountAmount = 0 };
        Assert.ThrowsAsync<BusinessRuleException>(async () => await _billService.CreateBillAsync(dto));
    }

    [Test]
    public void CreateBillAsync_InvalidTaxRateTooHigh_ThrowsBusinessRuleException()
    {
        var orderDto = new OrderDto { Id = 1, Status = (int)OrderStatus.Served, TotalAmount = 50, OrderItems = new [] { new OrderItemDto { MenuItemId = 1, Quantity = 1, UnitPrice = 50 } } };
        _orderServiceMock.Setup(s => s.GetOrderByIdAsync(1)).ReturnsAsync(orderDto);

        var dto = new BillCreateDto { OrderId = 1, TaxRate = 101, DiscountAmount = 0 };
        Assert.ThrowsAsync<BusinessRuleException>(async () => await _billService.CreateBillAsync(dto));
    }

    [Test]
    public void CreateBillAsync_InvalidDiscountNegative_ThrowsBusinessRuleException()
    {
        var orderDto = new OrderDto { Id = 1, Status = (int)OrderStatus.Served, TotalAmount = 50, OrderItems = new [] { new OrderItemDto { MenuItemId = 1, Quantity = 1, UnitPrice = 50 } } };
        _orderServiceMock.Setup(s => s.GetOrderByIdAsync(1)).ReturnsAsync(orderDto);

        var dto = new BillCreateDto { OrderId = 1, TaxRate = 5, DiscountAmount = -1 };
        Assert.ThrowsAsync<BusinessRuleException>(async () => await _billService.CreateBillAsync(dto));
    }

    [Test]
    public void CreateBillAsync_InvalidDiscountTooHigh_ThrowsBusinessRuleException()
    {
        var orderDto = new OrderDto { Id = 1, Status = (int)OrderStatus.Served, TotalAmount = 50, OrderItems = new [] { new OrderItemDto { MenuItemId = 1, Quantity = 1, UnitPrice = 50 } } };
        _orderServiceMock.Setup(s => s.GetOrderByIdAsync(1)).ReturnsAsync(orderDto);

        var dto = new BillCreateDto { OrderId = 1, TaxRate = 5, DiscountAmount = 51 }; // subtotal is 50
        Assert.ThrowsAsync<BusinessRuleException>(async () => await _billService.CreateBillAsync(dto));
    }

    [Test]
    public async Task GetAllBillsAsync_ReturnsAllBills()
    {
        _context.Bills.AddRange(
            new Bill { Id = 11, OrderId = 11, SubTotal = 20, TotalAmount = 20, Status = BillStatus.Pending },
            new Bill { Id = 12, OrderId = 12, SubTotal = 30, TotalAmount = 30, Status = BillStatus.Paid }
        );
        await _context.SaveChangesAsync();

        var result = await _billService.GetAllBillsAsync();
        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task UpdateBillAsync_ValidUpdate_Succeeds()
    {
        // Arrange
        var bill = new Bill { Id = 15, OrderId = 15, SubTotal = 50, TotalAmount = 50, Status = BillStatus.Pending };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        var orderDto = new OrderDto { Id = 15, Status = (int)OrderStatus.Served, TotalAmount = 50, OrderItems = new [] { new OrderItemDto { MenuItemId = 1, Quantity = 1, UnitPrice = 50 } } };
        _orderServiceMock.Setup(s => s.GetOrderByIdAsync(15)).ReturnsAsync(orderDto);

        var dto = new BillCreateDto { OrderId = 15, TaxRate = 10, DiscountAmount = 5 };

        // Act
        var result = await _billService.UpdateBillAsync(15, dto);

        // Assert
        Assert.That(result.TaxRate, Is.EqualTo(10));
        Assert.That(result.DiscountAmount, Is.EqualTo(5));
        Assert.That(result.TotalAmount, Is.EqualTo(50m + 5m - 5m));
    }

    [Test]
    public void UpdateBillAsync_OrderNotFound_ThrowsNotFoundException()
    {
        _orderServiceMock.Setup(s => s.GetOrderByIdAsync(15)).ReturnsAsync((OrderDto?)null);
        var dto = new BillCreateDto { OrderId = 15 };
        Assert.ThrowsAsync<NotFoundException>(async () => await _billService.UpdateBillAsync(1, dto));
    }

    [Test]
    public void UpdateBillAsync_BillNotFound_ThrowsNotFoundException()
    {
        var orderDto = new OrderDto { Id = 15, Status = (int)OrderStatus.Served, TotalAmount = 50, OrderItems = new [] { new OrderItemDto { MenuItemId = 1, Quantity = 1, UnitPrice = 50 } } };
        _orderServiceMock.Setup(s => s.GetOrderByIdAsync(15)).ReturnsAsync(orderDto);
        var dto = new BillCreateDto { OrderId = 15 };
        Assert.ThrowsAsync<NotFoundException>(async () => await _billService.UpdateBillAsync(999, dto));
    }

    [Test]
    public void UpdateBillStatusAsync_InvalidStatus_ThrowsBusinessRuleException()
    {
        Assert.ThrowsAsync<BusinessRuleException>(async () => await _billService.UpdateBillStatusAsync(1, "InvalidStatusName"));
    }

    [Test]
    public void UpdateBillStatusAsync_BillNotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _billService.UpdateBillStatusAsync(999, "Paid"));
    }

    [Test]
    public async Task UpdateBillStatusAsync_TransitionToFailed_Succeeds()
    {
        var bill = new Bill { Id = 20, OrderId = 20, SubTotal = 40, TotalAmount = 40, Status = BillStatus.Pending };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        var success = await _billService.UpdateBillStatusAsync(20, "Failed");
        Assert.That(success, Is.True);
        var updated = await _context.Bills.FindAsync(20);
        Assert.That(updated!.Status, Is.EqualTo(BillStatus.Failed));
    }

    [Test]
    public async Task UpdateBillStatusAsync_TransitionToPaid_SignalError_StillSucceeds()
    {
        var bill = new Bill { Id = 21, OrderId = 21, SubTotal = 40, TotalAmount = 40, Status = BillStatus.Pending };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        _orderServiceMock.Setup(s => s.UpdateOrderStatusAsync(21, (int)OrderStatus.Completed)).ReturnsAsync(true);
        _orderServiceMock.Setup(s => s.GetOrderByIdAsync(21)).ReturnsAsync(new OrderDto { Id = 21, TableId = 5 });
        _signalServiceMock.Setup(s => s.NotifyBillPaidAsync(5, It.IsAny<BillDto>())).ThrowsAsync(new Exception("SignalR offline"));

        var success = await _billService.UpdateBillStatusAsync(21, "Paid");
        Assert.That(success, Is.True);
        var updated = await _context.Bills.FindAsync(21);
        Assert.That(updated!.Status, Is.EqualTo(BillStatus.Paid));
    }

    [Test]
    public void UpdateBillStatusAsync_InvalidTransition_ThrowsBusinessRuleException()
    {
        var bill = new Bill { Id = 22, OrderId = 22, SubTotal = 40, TotalAmount = 40, Status = BillStatus.Pending };
        _context.Bills.Add(bill);
        _context.SaveChanges();

        Assert.ThrowsAsync<BusinessRuleException>(async () => await _billService.UpdateBillStatusAsync(22, "Pending"));
    }

    [Test]
    public async Task GenerateBillPdfAsync_ReturnsPdfBytes()
    {
        var bill = new Bill { Id = 23, OrderId = 23, SubTotal = 40, TotalAmount = 40, Status = BillStatus.Pending };
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();

        var orderDto = new OrderDto { Id = 23, Status = (int)OrderStatus.Served, TotalAmount = 40, OrderItems = new [] { new OrderItemDto { MenuItemId = 1, Quantity = 1, UnitPrice = 40 } } };
        _orderServiceMock.Setup(s => s.GetOrderByIdAsync(23)).ReturnsAsync(orderDto);

        var result = await _billService.GenerateBillPdfAsync(23);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void UpdateBillStatusAsync_ConcurrentDeletion_ThrowsNotFoundException()
    {
        var repoMock = new Mock<IBillRepository>();
        var bill = new Bill { Id = 35, OrderId = 35, Status = BillStatus.Pending };
        repoMock.Setup(r => r.GetByIdAsync(35)).ReturnsAsync(bill);
        repoMock.Setup(r => r.UpdateStatusAsync(35, BillStatus.Paid)).ReturnsAsync(false);

        var service = new BillService(repoMock.Object, _orderServiceMock.Object, _signalServiceMock.Object, new Mock<ILogger<BillService>>().Object);

        Assert.ThrowsAsync<NotFoundException>(async () => await service.UpdateBillStatusAsync(35, "Paid"));
    }
}
