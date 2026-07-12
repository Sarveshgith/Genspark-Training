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
public class TableServiceTest
{
    private AppDbContext _context = null!;
    private ITableRepository _tableRepository = null!;
    private Mock<ISignalService> _signalServiceMock = null!;
    private ITableService _tableService = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _tableRepository = new TableRepository(_context);

        var logger = new Mock<ILogger<TableService>>().Object;
        _signalServiceMock = new Mock<ISignalService>();
        _tableService = new TableService(_tableRepository, _context, _signalServiceMock.Object, logger);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task CreateAsync_PassTest()
    {
        var dto = new TableCreateDto
        {
            Number = 10,
            Capacity = 4,
            Status = (int)TableStatus.Available
        };

        var result = await _tableService.CreateAsync(dto);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Number, Is.EqualTo(10));
        Assert.That(result.Capacity, Is.EqualTo(4));
    }

    [Test]
    public async Task CreateAsync_FailTest_DuplicateNumber_ThrowsConflictException()
    {
        // Seed table
        _context.Tables.Add(new Table { Id = 1, Number = 10, Capacity = 4, Status = TableStatus.Available });
        await _context.SaveChangesAsync();

        var dto = new TableCreateDto
        {
            Number = 10, // Duplicate
            Capacity = 6
        };

        Assert.ThrowsAsync<ConflictException>(async () => await _tableService.CreateAsync(dto));
    }

    [Test]
    public async Task GetByIdAsync_PassTest()
    {
        _context.Tables.Add(new Table { Id = 5, Number = 12, Capacity = 2, Status = TableStatus.Available });
        await _context.SaveChangesAsync();

        var result = await _tableService.GetByIdAsync(5);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(5));
        Assert.That(result.Number, Is.EqualTo(12));
    }

    [Test]
    public void GetByIdAsync_FailTest_NotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _tableService.GetByIdAsync(999));
    }

    [Test]
    public async Task ChangeStatusAsync_PassTest()
    {
        _context.Tables.Add(new Table { Id = 2, Number = 5, Capacity = 4, Status = TableStatus.Available });
        await _context.SaveChangesAsync();

        var success = await _tableService.ChangeStatusAsync(2, (int)TableStatus.Occupied);

        Assert.That(success, Is.True);
        var updated = await _context.Tables.FindAsync(2);
        Assert.That(updated!.Status, Is.EqualTo(TableStatus.Occupied));
    }

    [Test]
    public void ChangeStatusAsync_FailTest_InvalidEnum_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(async () => await _tableService.ChangeStatusAsync(1, 99)); // Invalid table status enum
    }

    [Test]
    public async Task RegenerateSecretAsync_PassTest_UpdatesSecret()
    {
        var table = new Table { Id = 3, Number = 8, Capacity = 2, Status = TableStatus.Available, Secret = "old-secret" };
        _context.Tables.Add(table);
        await _context.SaveChangesAsync();

        var success = await _tableService.RegenerateSecretAsync(3);

        Assert.That(success, Is.True);
        var updated = await _context.Tables.FindAsync(3);
        Assert.That(updated!.Secret, Is.Not.EqualTo("old-secret"));
        Assert.That(updated.Secret, Is.Not.Null.Or.Empty);
    }

    [Test]
    public void RegenerateSecretAsync_FailTest_NotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _tableService.RegenerateSecretAsync(999));
    }

    [Test]
    public async Task GetAllAsync_NoActiveOrders_ReturnsAllTables()
    {
        // Arrange
        _context.Tables.AddRange(
            new Table { Id = 100, Number = 100, Capacity = 2, Status = TableStatus.Available },
            new Table { Id = 101, Number = 101, Capacity = 4, Status = TableStatus.Occupied }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _tableService.GetAllAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        var table100 = result.First(t => t.Id == 100);
        Assert.That(table100.ActiveOrderId, Is.Null);
    }

    [Test]
    public async Task GetAllAsync_WithActiveOrders_ReturnsTablesWithActiveOrderId()
    {
        // Arrange
        _context.Tables.AddRange(
            new Table { Id = 102, Number = 102, Capacity = 2, Status = TableStatus.Occupied },
            new Table { Id = 103, Number = 103, Capacity = 4, Status = TableStatus.Available }
        );
        _context.Orders.Add(
            new Order { Id = 50, TableId = 102, Status = OrderStatus.Pending, TotalAmount = 25 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _tableService.GetAllAsync();

        // Assert
        var table102 = result.First(t => t.Id == 102);
        Assert.That(table102.ActiveOrderId, Is.EqualTo(50));
        var table103 = result.First(t => t.Id == 103);
        Assert.That(table103.ActiveOrderId, Is.Null);
    }

    [Test]
    public async Task GetByIdAsync_WithActiveOrder_ReturnsTableWithActiveOrderId()
    {
        // Arrange
        _context.Tables.Add(new Table { Id = 104, Number = 104, Capacity = 2, Status = TableStatus.Occupied });
        _context.Orders.Add(new Order { Id = 51, TableId = 104, Status = OrderStatus.InPrep, TotalAmount = 30 });
        await _context.SaveChangesAsync();

        // Act
        var result = await _tableService.GetByIdAsync(104);

        // Assert
        Assert.That(result.ActiveOrderId, Is.EqualTo(51));
        Assert.That(result.ActiveOrderCreatedAt, Is.Not.Null);
    }

    [Test]
    public async Task CreateAsync_StatusNotHasValue_UsesDefaultAvailable()
    {
        // Arrange
        var dto = new TableCreateDto
        {
            Number = 105,
            Capacity = 2,
            Status = null
        };

        // Act
        var result = await _tableService.CreateAsync(dto);

        // Assert
        Assert.That(result.Status, Is.EqualTo((int)TableStatus.Available));
    }

    [Test]
    public async Task UpdateAsync_ValidUpdate_Succeeds()
    {
        // Arrange
        _context.Tables.Add(new Table { Id = 106, Number = 106, Capacity = 2, Status = TableStatus.Available });
        await _context.SaveChangesAsync();

        var dto = new TableUpdateDto
        {
            Number = 107,
            Capacity = 4
        };

        // Act
        var result = await _tableService.UpdateAsync(106, dto);

        // Assert
        Assert.That(result.Number, Is.EqualTo(107));
        Assert.That(result.Capacity, Is.EqualTo(4));
    }

    [Test]
    public void UpdateAsync_NotFound_ThrowsNotFoundException()
    {
        var dto = new TableUpdateDto { Number = 108, Capacity = 2 };
        Assert.ThrowsAsync<NotFoundException>(async () => await _tableService.UpdateAsync(999, dto));
    }

    [Test]
    public async Task UpdateAsync_DuplicateNumber_ThrowsConflictException()
    {
        // Arrange
        _context.Tables.AddRange(
            new Table { Id = 109, Number = 109, Capacity = 2, Status = TableStatus.Available },
            new Table { Id = 110, Number = 110, Capacity = 4, Status = TableStatus.Available }
        );
        await _context.SaveChangesAsync();

        var dto = new TableUpdateDto { Number = 110, Capacity = 2 }; // Conflict with table 110

        // Act & Assert
        Assert.ThrowsAsync<ConflictException>(async () => await _tableService.UpdateAsync(109, dto));
    }

    [Test]
    public void ChangeStatusAsync_NotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _tableService.ChangeStatusAsync(999, (int)TableStatus.Available));
    }

    [Test]
    public async Task ChangeStatusAsync_SignalError_StillSucceeds()
    {
        // Arrange
        _context.Tables.Add(new Table { Id = 111, Number = 111, Capacity = 2, Status = TableStatus.Available });
        await _context.SaveChangesAsync();

        _signalServiceMock.Setup(s => s.NotifyTablesUpdatedAsync()).ThrowsAsync(new Exception("SignalR connection failed"));

        // Act
        var result = await _tableService.ChangeStatusAsync(111, (int)TableStatus.Occupied);

        // Assert
        Assert.That(result, Is.True);
        var updated = await _context.Tables.FindAsync(111);
        Assert.That(updated!.Status, Is.EqualTo(TableStatus.Occupied));
    }

    [Test]
    public async Task DeleteAsync_ValidId_Succeeds()
    {
        // Arrange
        _context.Tables.Add(new Table { Id = 112, Number = 112, Capacity = 2, Status = TableStatus.Available });
        await _context.SaveChangesAsync();

        // Act
        var result = await _tableService.DeleteAsync(112);

        // Assert
        Assert.That(result, Is.True);
        var updated = await _context.Tables.FindAsync(112);
        Assert.That(updated!.IsDeleted, Is.True);
    }

    [Test]
    public void DeleteAsync_NotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _tableService.DeleteAsync(999));
    }

    [Test]
    public async Task GetTableSecretAsync_ValidId_ReturnsSecret()
    {
        // Arrange
        _context.Tables.Add(new Table { Id = 113, Number = 113, Capacity = 2, Status = TableStatus.Available, Secret = "mysecret" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _tableService.GetTableSecretAsync(113);

        // Assert
        Assert.That(result, Is.EqualTo("mysecret"));
    }

    [Test]
    public void GetTableSecretAsync_NotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(async () => await _tableService.GetTableSecretAsync(999));
    }
}
