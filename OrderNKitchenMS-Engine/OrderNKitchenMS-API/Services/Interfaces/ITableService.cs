using OrderNKitchenMS_API.Models.DTOs;

namespace OrderNKitchenMS_API.Services.Interfaces;

public interface ITableService
{
    public Task<IEnumerable<TableDto>> GetAllAsync();

    public Task<TableDto> GetByIdAsync(int id);

    public Task<TableDto> CreateAsync(TableCreateDto tableCreateDto);

    public Task<TableDto> UpdateAsync(int id, TableUpdateDto tableUpdateDto);

    public Task<bool> ChangeStatusAsync(int id, int status);

    public Task<bool> DeleteAsync(int id);
}
