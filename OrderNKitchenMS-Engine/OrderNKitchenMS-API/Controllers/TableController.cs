// @feature Backend API | Table Administration | Manages restaurant table listings, dynamic capacity checks, and seat allocations.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using OrderNKitchenMS_API.Utils;

namespace OrderNKitchenMS_API.Controllers;

[ApiController]
[Route("api/tables")]
[Authorize]
public class TableController : ControllerBase
{
    private readonly ITableService _tableService;
    private readonly IConfiguration _configuration;

    public TableController(ITableService tableService, IConfiguration configuration)
    {
        _tableService = tableService;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpGet("qrcode")]
    public async Task<ActionResult> GetTableQRCode([FromQuery] int tableId)
    {
        await _tableService.GetByIdAsync(tableId);

        var baseUrl = _configuration["QrSettings:BaseUrl"] ?? "http://localhost:4200";
        baseUrl = baseUrl.TrimEnd('/');
        var qrUrl = $"{baseUrl}?tableId={tableId}";
        
        byte[] qrBytes = QRCodeHelper.GenerateQRCodeBytes(qrUrl);
        return File(qrBytes, "image/png");
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TableDto>>> GetTables()
    {
        var tables = await _tableService.GetAllAsync();
        return Ok(tables);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TableDto>> GetTableById(int id)
    {
        var table = await _tableService.GetByIdAsync(id);
        return Ok(table);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<ActionResult<TableDto>> CreateTable([FromBody] TableCreateDto tableCreateDto)
    {
        var createdTable = await _tableService.CreateAsync(tableCreateDto);
        return CreatedAtAction(nameof(GetTableById), new { id = createdTable.Id }, createdTable);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<TableDto>> UpdateTable(int id, [FromBody] TableUpdateDto tableUpdateDto)
    {
        var updatedTable = await _tableService.UpdateAsync(id, tableUpdateDto);
        return Ok(updatedTable);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult> ChangeStatus(int id, [FromBody] TableStatusUpdateDto payload)
    {
        Validation.RequireNotNull(payload, nameof(payload), "Status payload is required.");
        await _tableService.ChangeStatusAsync(id, payload.Status);
        return NoContent();
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteTable(int id)
    {
        await _tableService.DeleteAsync(id);
        return NoContent();
    }
}
