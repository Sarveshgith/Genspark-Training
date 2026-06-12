using System;
using Microsoft.EntityFrameworkCore;
using OrderNKitchenMS_API.Data;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Models.Enums;
using OrderNKitchenMS_API.Repositories.Interfaces;

namespace OrderNKitchenMS_API.Repositories;

public class TableRepository : ITableRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<Table> _tables;

    public TableRepository(AppDbContext context)
    {
        _context = context;
        _tables = _context.Tables;
    }

    public async Task<IEnumerable<Table>> GetAllAsync()
    {
        return await _tables
            .Where(table => !table.IsDeleted)
            .OrderBy(table => table.Number)
            .ToListAsync();
    }

    public async Task<Table?> GetByIdAsync(int id)
    {
        return await _tables
            .FirstOrDefaultAsync(table => table.Id == id && !table.IsDeleted);
    }

    public async Task<Table?> GetByNumberAsync(int number, int? excludeId = null)
    {
        return await _tables.FirstOrDefaultAsync(table =>
            table.Number == number &&
            (!excludeId.HasValue || table.Id != excludeId.Value));
    }

    public async Task<Table> CreateAsync(Table table)
    {
        _tables.Add(table);
        await _context.SaveChangesAsync();
        return table;
    }

    public async Task<Table?> UpdateAsync(int id, Table table)
    {
        var existingTable = await GetByIdAsync(id);
        if (existingTable == null)
        {
            return null;
        }

        existingTable.Number = table.Number;
        existingTable.Capacity = table.Capacity;
        await _context.SaveChangesAsync();
        return existingTable;
    }

    public async Task<bool> ChangeStatusAsync(int id, TableStatus status)
    {
        var existingTable = await GetByIdAsync(id);
        if (existingTable == null)
        {
            return false;
        }

        existingTable.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }

    //Soft-deleting
    public async Task<bool> DeleteAsync(int id)
    {
        var table = await GetByIdAsync(id);
        if (table == null)
        {
            return false;
        }

        table.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }
}
