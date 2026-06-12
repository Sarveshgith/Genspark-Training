using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Models.Enums;

namespace OrderNKitchenMS_API.Repositories.Interfaces;

public interface ITableRepository
{
    public Task<IEnumerable<Table>> GetAllAsync();

    public Task<Table?> GetByIdAsync(int id);

    public Task<Table?> GetByNumberAsync(int number, int? excludeId = null);

    public Task<Table> CreateAsync(Table table);

    public Task<Table?> UpdateAsync(int id, Table table);

    public Task<bool> ChangeStatusAsync(int id, TableStatus status);

    public Task<bool> DeleteAsync(int id);
}
