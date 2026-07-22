using System;
using Microsoft.EntityFrameworkCore;
using OrderNKitchenMS_API.Data;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Models.Enums;
using OrderNKitchenMS_API.Repositories.Interfaces;

namespace OrderNKitchenMS_API.Repositories;

public class BillRepository : IBillRepository
{
    private readonly AppDbContext _context;

    public BillRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Bill> CreateBillAsync(Bill bill)
    {
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();
        return bill;
    }

    public async Task<Bill?> GetBillByOrderIdAsync(int orderId)
    {
        var bill = await _context.Bills.Include(b => b.Splits).FirstOrDefaultAsync(b => b.OrderId == orderId);
        if (bill == null)
        {
            return null;
        }
        return bill;
    }

    public async Task<IEnumerable<Bill>> GetAllBillsAsync()
    {
        return await _context.Bills.ToListAsync();
    }

    public async Task<Bill?> GetByIdAsync(int id)
    {
        return await _context.Bills.Include(b => b.Splits).FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Bill?> UpdateBillAsync(int id, Bill updatedBill)
    {
        var existingBill = await _context.Bills.FindAsync(id);
        if (existingBill == null)
        {
            return null;
        }

        existingBill.OrderId = updatedBill.OrderId;
        existingBill.SubTotal = updatedBill.SubTotal;
        existingBill.TaxRate = updatedBill.TaxRate;
        existingBill.DiscountAmount = updatedBill.DiscountAmount;
        existingBill.TotalAmount = updatedBill.TotalAmount;
        existingBill.Status = updatedBill.Status;

        await _context.SaveChangesAsync();
        return existingBill;
    }

    public async Task<bool> UpdateStatusAsync(int id, BillStatus newStatus)
    {
        var bill = await _context.Bills.FindAsync(id);
        if (bill == null)
        {
            return false;
        }

        bill.Status = newStatus;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<BillSplit?> GetBillSplitByIdAsync(int splitId)
    {
        return await _context.BillSplits.Include(bs => bs.Bill).ThenInclude(b => b.Splits).FirstOrDefaultAsync(bs => bs.Id == splitId);
    }

    public async Task<bool> UpdateBillSplitStatusAsync(int splitId, BillStatus status)
    {
        var split = await _context.BillSplits.FindAsync(splitId);
        if (split == null)
        {
            return false;
        }

        split.Status = status;
        await _context.SaveChangesAsync();
        return true;
    }
}
