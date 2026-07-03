using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Models.Enums;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OrderNKitchenMS_API.Data;

namespace OrderNKitchenMS_API.Services;

public class TableService : ITableService
{
    private readonly ITableRepository _tableRepository;
    private readonly AppDbContext _context;
    private readonly ISignalService _signalService;
    private readonly ILogger<TableService> _logger;

    public TableService(
        ITableRepository tableRepository, 
        AppDbContext context,
        ISignalService signalService,
        ILogger<TableService> logger)
    {
        _tableRepository = tableRepository;
        _context = context;
        _signalService = signalService;
        _logger = logger;
    }

    // Retrieves all tables.
    public async Task<IEnumerable<TableDto>> GetAllAsync()
    {
        _logger.LogInformation("GetAllAsync called for tables");
        var tables = (await _tableRepository.GetAllAsync()).ToList();

        var tableIds = tables.Select(t => t.Id).ToList();

        var activeOrders = await _context.Orders
            .Where(o => tableIds.Contains(o.TableId) && o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
            .ToListAsync();

        var activeOrdersMap = activeOrders
            .GroupBy(o => o.TableId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(o => o.CreatedAt).First());

        return tables.Select(table => {
            activeOrdersMap.TryGetValue(table.Id, out var activeOrder);
            var dto = MapTableToDto(table);
            dto.ActiveOrderId = activeOrder?.Id;
            dto.ActiveOrderCreatedAt = activeOrder?.CreatedAt;
            return dto;
        });
    }

    // Retrieves a specific table by its unique identifier.
    public async Task<TableDto> GetByIdAsync(int id)
    {
        _logger.LogInformation("GetByIdAsync called for Table ID: {Id}", id);
        var table = await _tableRepository.GetByIdAsync(id);
        if (table == null)
        {
            _logger.LogWarning("GetByIdAsync failed: Table with ID {Id} was not found.", id);
            throw new NotFoundException($"Table with id {id} was not found.");
        }

        var activeOrder = await _context.Orders
            .Where(o => o.TableId == table.Id && o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        var dto = MapTableToDto(table);
        dto.ActiveOrderId = activeOrder?.Id;
        dto.ActiveOrderCreatedAt = activeOrder?.CreatedAt;
        return dto;
    }

    // Creates a new dining table.
    public async Task<TableDto> CreateAsync(TableCreateDto tableCreateDto)
    {
        _logger.LogInformation("CreateAsync started for Table Number: {Number}", tableCreateDto.Number);
        ValidateCreateDto(tableCreateDto);
        await EnsureUniqueNumberAsync(tableCreateDto.Number);
        var tableEntity = MapCreateDtoToEntity(tableCreateDto);
        tableEntity.Secret = GenerateTableSecret();
        var createdTable = await _tableRepository.CreateAsync(tableEntity);
        _logger.LogInformation("CreateAsync succeeded. Created Table ID: {Id} for Number: {Number}", createdTable.Id, createdTable.Number);
        return MapTableToDto(createdTable);
    }

    // Updates the details of an existing dining table.
    public async Task<TableDto> UpdateAsync(int id, TableUpdateDto tableUpdateDto)
    {
        _logger.LogInformation("UpdateAsync started for Table ID: {Id}", id);
        ValidateUpdateDto(tableUpdateDto);
        await EnsureUniqueNumberAsync(tableUpdateDto.Number, id);
        var tableEntity = MapUpdateDtoToEntity(tableUpdateDto);
        var updatedTable = await _tableRepository.UpdateAsync(id, tableEntity);
        if (updatedTable == null)
        {
            _logger.LogWarning("UpdateAsync failed: Table with ID {Id} was not found.", id);
            throw new NotFoundException($"Table with id {id} was not found.");
        }

        _logger.LogInformation("UpdateAsync succeeded for Table ID: {Id}", id);
        return MapTableToDto(updatedTable);
    }

    // Changes the status (e.g. Occupied, Available) of a specific table.
    public async Task<bool> ChangeStatusAsync(int id, int status)
    {
        _logger.LogInformation("ChangeStatusAsync started for Table ID: {Id}, new status: {Status}", id, status);
        EnsureValidStatus(status);

        var isChanged = await _tableRepository.ChangeStatusAsync(id, (TableStatus)status);
        if (!isChanged)
        {
            _logger.LogWarning("ChangeStatusAsync failed: Table with ID {Id} was not found.", id);
            throw new NotFoundException($"Table with id {id} was not found.");
        }

        try
        {
            await _signalService.NotifyTablesUpdatedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast table update signal.");
        }

        _logger.LogInformation("ChangeStatusAsync succeeded for Table ID: {Id}", id);
        return true;
    }

    // Marks a specific table as deleted.
    public async Task<bool> DeleteAsync(int id)
    {
        _logger.LogInformation("DeleteAsync started for Table ID: {Id}", id);
        var isDeleted = await _tableRepository.DeleteAsync(id);
        if (!isDeleted)
        {
            _logger.LogWarning("DeleteAsync failed: Table with ID {Id} was not found.", id);
            throw new NotFoundException($"Table with id {id} was not found.");
        }

        _logger.LogInformation("DeleteAsync succeeded for Table ID: {Id}", id);
        return true;
    }

    // Rotates the secret of an existing table.
    public async Task<bool> RegenerateSecretAsync(int id)
    {
        _logger.LogInformation("RegenerateSecretAsync started for Table ID: {Id}", id);
        var newSecret = GenerateTableSecret();
        var isUpdated = await _tableRepository.UpdateSecretAsync(id, newSecret);
        if (!isUpdated)
        {
            _logger.LogWarning("RegenerateSecretAsync failed: Table with ID {Id} was not found.", id);
            throw new NotFoundException($"Table with id {id} was not found.");
        }

        _logger.LogInformation("RegenerateSecretAsync succeeded. Rotated secret for Table ID: {Id}", id);
        return true;
    }

    private static string GenerateTableSecret()
    {
        return Guid.NewGuid().ToString("N");
    }

    public async Task<string> GetTableSecretAsync(int id)
    {
        var table = await _tableRepository.GetByIdAsync(id);
        if (table == null)
        {
            throw new NotFoundException($"Table with id {id} was not found.");
        }
        return table.Secret;
    }

    private static void ValidateCreateDto(TableCreateDto tableCreateDto)
    {
        Validation.RequireNotNull(tableCreateDto, nameof(tableCreateDto), "Table data is required.");

        if (tableCreateDto.Status.HasValue)
        {
            EnsureValidStatus(tableCreateDto.Status.Value);
        }
    }

    private static void ValidateUpdateDto(TableUpdateDto tableUpdateDto)
    {
        Validation.RequireNotNull(tableUpdateDto, nameof(tableUpdateDto), "Table data is required.");
    }

    private static void EnsureValidStatus(int status)
    {
        Validation.RequireValidEnum<TableStatus>(status, nameof(status), "Invalid table status.");
    }

    private async Task EnsureUniqueNumberAsync(int number, int? excludeId = null)
    {
        var existingTable = await _tableRepository.GetByNumberAsync(number, excludeId);
        if (existingTable != null)
        {
            _logger.LogWarning("Unique table number check failed: Table number {Number} already exists.", number);
            throw new ConflictException($"Table number {number} already exists.");
        }
    }

    private static TableDto MapTableToDto(Table table)
    {
        return new TableDto
        {
            Id = table.Id,
            Number = table.Number,
            Capacity = table.Capacity,
            Status = (int)table.Status,
            StatusName = table.Status.ToString(),
            IsDeleted = table.IsDeleted
        };
    }

    private static Table MapCreateDtoToEntity(TableCreateDto tableCreateDto)
    {
        return new Table
        {
            Number = tableCreateDto.Number,
            Capacity = tableCreateDto.Capacity,
            Status = tableCreateDto.Status.HasValue
                ? (TableStatus)tableCreateDto.Status.Value
                : TableStatus.Available
        };
    }

    private static Table MapUpdateDtoToEntity(TableUpdateDto tableUpdateDto)
    {
        return new Table
        {
            Number = tableUpdateDto.Number,
            Capacity = tableUpdateDto.Capacity
        };
    }
}
