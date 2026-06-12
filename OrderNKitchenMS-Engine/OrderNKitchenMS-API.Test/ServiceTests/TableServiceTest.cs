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
    private ITableService _tableService = null!;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "OrderNKitchenDb")
            .Options;

        _context = new AppDbContext(options);
        _tableRepository = new TableRepository(_context);

        var logger = new Mock<ILogger<TableService>>().Object;
        _tableService = new TableService(_tableRepository, logger);
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
}
