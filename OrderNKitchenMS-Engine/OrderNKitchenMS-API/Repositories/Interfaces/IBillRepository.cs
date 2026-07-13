using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Models.Enums;

namespace OrderNKitchenMS_API.Repositories.Interfaces;

public interface IBillRepository
{
    public Task<Bill> CreateBillAsync(Bill bill);

    public Task<Bill?> GetBillByOrderIdAsync(int orderId);

    public Task<IEnumerable<Bill>> GetAllBillsAsync();

    public Task<Bill?> UpdateBillAsync(int id, Bill updatedBill);

    public Task<bool> UpdateStatusAsync(int id, BillStatus newStatus);

    public Task<Bill?> GetByIdAsync(int id);
    
    public Task<BillSplit?> GetBillSplitByIdAsync(int splitId);
    
    public Task<bool> UpdateBillSplitStatusAsync(int splitId, BillStatus status);
}
