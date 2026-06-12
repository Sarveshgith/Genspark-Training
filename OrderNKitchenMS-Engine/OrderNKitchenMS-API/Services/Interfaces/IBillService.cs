using System;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Models.Enums;

namespace OrderNKitchenMS_API.Services.Interfaces;

public interface IBillService
{
    public Task<BillDto> CreateBillAsync(BillCreateDto billCreateDto);

    public Task<BillDto> GetBillByOrderIdAsync(int orderId);

    public Task<IEnumerable<BillDto>> GetAllBillsAsync();

    public Task<BillDto> UpdateBillAsync(int id, BillCreateDto updatedBill);

    public Task<bool> UpdateBillStatusAsync(int billId, string newStatus);

    public Task<byte[]> GenerateBillPdfAsync(int orderId);
}
