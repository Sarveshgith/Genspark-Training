using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Utils;

namespace OrderNKitchenMS_API.Controllers;

[ApiController]
[Route("api/tables")]
[Authorize]
public class TableController : ControllerBase
{
    private readonly ITableService _tableService;

    public TableController(ITableService tableService)
    {
        _tableService = tableService;
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
