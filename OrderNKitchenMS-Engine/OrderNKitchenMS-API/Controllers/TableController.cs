// @feature Backend API | Table Administration | Manages restaurant table listings, dynamic capacity checks, and seat allocations.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Models.Enums;
using OrderNKitchenMS_API.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderNKitchenMS_API.Utils;
using OrderNKitchenMS_API.Data;
using OrderNKitchenMS_API.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace OrderNKitchenMS_API.Controllers;

[ApiController]
[Route("api/tables")]
[Authorize]
public class TableController : ControllerBase
{
    private readonly ITableService _tableService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TableController> _logger;

    public TableController(ITableService tableService, IConfiguration configuration, ILogger<TableController> logger)
    {
        _tableService = tableService;
        _configuration = configuration;
        _logger = logger;
    }

    [Authorize(Policy = "AdminOrWaiter")]
    [HttpGet("qrcode")]
    public async Task<ActionResult> GetTableQRCode([FromQuery] int tableId)
    {
        Validation.ValidateId(tableId, nameof(tableId));
        _logger.LogInformation("GetTableQRCode requested for Table ID: {TableId}", tableId);

        var secret = await _tableService.GetTableSecretAsync(tableId);

        var baseUrl = _configuration["QrSettings:BaseUrl"] ?? "http://localhost:4200";
        baseUrl = baseUrl.TrimEnd('/');
        var qrUrl = $"{baseUrl}/guest/{secret}";
        
        byte[] qrBytes = QRCodeHelper.GenerateQRCodeBytes(qrUrl);
        return File(qrBytes, "image/png");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TableDto>>> GetTables()
    {
        _logger.LogInformation("GetTables requested");
        var tables = await _tableService.GetAllAsync();
        return Ok(tables);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TableDto>> GetTableById(int id)
    {
        Validation.ValidateId(id, nameof(id));
        _logger.LogInformation("GetTableById requested for ID: {Id}", id);
        var table = await _tableService.GetByIdAsync(id);
        return Ok(table);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<ActionResult<TableDto>> CreateTable([FromBody] TableCreateDto tableCreateDto)
    {
        Validation.RequireNotNull(tableCreateDto, nameof(tableCreateDto));
        _logger.LogInformation("CreateTable requested for Number: {Number}, Capacity: {Capacity}", tableCreateDto.Number, tableCreateDto.Capacity);
        var createdTable = await _tableService.CreateAsync(tableCreateDto);
        return CreatedAtAction(nameof(GetTableById), new { id = createdTable.Id }, createdTable);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<TableDto>> UpdateTable(int id, [FromBody] TableUpdateDto tableUpdateDto)
    {
        Validation.ValidateId(id, nameof(id));
        Validation.RequireNotNull(tableUpdateDto, nameof(tableUpdateDto));
        _logger.LogInformation("UpdateTable requested for ID: {Id}", id);
        var updatedTable = await _tableService.UpdateAsync(id, tableUpdateDto);
        return Ok(updatedTable);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult> ChangeStatus(int id, [FromBody] TableStatusUpdateDto payload)
    {
        Validation.ValidateId(id, nameof(id));
        Validation.RequireNotNull(payload, nameof(payload), "Status payload is required.");
        Validation.RequireValidEnum<TableStatus>(payload.Status, nameof(payload.Status));

        _logger.LogInformation("ChangeStatus requested for Table ID: {Id}, Status: {Status}", id, payload.Status);
        await _tableService.ChangeStatusAsync(id, payload.Status);
        return NoContent();
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteTable(int id)
    {
        Validation.ValidateId(id, nameof(id));
        _logger.LogInformation("DeleteTable requested for ID: {Id}", id);
        await _tableService.DeleteAsync(id);
        return NoContent();
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPatch("{id:int}/regenerate-secret")]
    public async Task<ActionResult> RegenerateSecret(int id)
    {
        Validation.ValidateId(id, nameof(id));
        _logger.LogInformation("RegenerateSecret requested for Table ID: {Id}", id);
        await _tableService.RegenerateSecretAsync(id);
        return NoContent();
    }
}
