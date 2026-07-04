using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Utils;
using Microsoft.Extensions.Logging;

namespace OrderNKitchenMS_API.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Policy = "AdminOrChef")]
public class ItemController : ControllerBase
{
    private readonly IItemService _itemService;
    private readonly ILogger<ItemController> _logger;

    public ItemController(IItemService itemService, ILogger<ItemController> logger)
    {
        _itemService = itemService;
        _logger = logger;
    }

    [HttpGet("items")]
    public async Task<ActionResult<IEnumerable<ItemDto>>> GetItems()
    {
        _logger.LogInformation("GetItems requested.");
        var items = await _itemService.GetAllItemsAsync();
        return Ok(items);
    }

    [HttpGet("items/{id:int}")]
    public async Task<ActionResult<ItemDto>> GetItemById(int id)
    {
        Validation.ValidateId(id);
        _logger.LogInformation("GetItemById requested for ID: {Id}", id);
        var item = await _itemService.GetItemByIdAsync(id);
        return Ok(item);
    }

    [HttpGet("items/low-stock")]
    public async Task<ActionResult<IEnumerable<ItemDto>>> GetLowStock()
    {
        _logger.LogInformation("GetLowStock requested.");
        var lowStock = await _itemService.GetLowStockItemsAsync();
        return Ok(lowStock);
    }

    [HttpPost("items")]
    public async Task<ActionResult<ItemDto>> CreateItem([FromBody] ItemCreateDto dto)
    {
        Validation.RequireNotNull(dto, nameof(dto));
        _logger.LogInformation("CreateItem requested for Item: '{Name}'", dto.Name);
        var created = await _itemService.CreateItemAsync(dto);
        return CreatedAtAction(nameof(GetItemById), new { id = created.Id }, created);
    }

    [HttpPut("items/{id:int}")]
    public async Task<ActionResult<ItemDto>> UpdateItem(int id, [FromBody] ItemUpdateDto dto)
    {
        Validation.ValidateRequest(id, dto);
        _logger.LogInformation("UpdateItem requested for ID: {Id}", id);
        var updated = await _itemService.UpdateItemAsync(id, dto);
        return Ok(updated);
    }

    [HttpPatch("items/{id:int}/restock")]
    public async Task<ActionResult<ItemDto>> RestockItem(int id, [FromBody] ItemRestockDto dto)
    {
        Validation.ValidateRequest(id, dto);
        _logger.LogInformation("RestockItem requested for ID: {Id}", id);
        var updated = await _itemService.RestockItemAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("items/{id:int}")]
    public async Task<ActionResult> DeleteItem(int id)
    {
        Validation.ValidateId(id);
        _logger.LogInformation("DeleteItem requested for ID: {Id}", id);
        await _itemService.ChangeItemStatusAsync(id, false);
        return NoContent();
    }

    [HttpGet("menu-items/{menuItemId:int}/ingredients")]
    public async Task<ActionResult<IEnumerable<MenuItemIngredientDto>>> GetIngredientsByMenuItemId(int menuItemId)
    {
        Validation.ValidateId(menuItemId, nameof(menuItemId));
        _logger.LogInformation("GetIngredientsByMenuItemId requested for MenuItem ID: {MenuItemId}", menuItemId);
        var ingredients = await _itemService.GetIngredientsByMenuItemIdAsync(menuItemId);
        return Ok(ingredients);
    }

    [HttpPost("menu-items/{menuItemId:int}/ingredients")]
    public async Task<ActionResult<IEnumerable<MenuItemIngredientDto>>> AddMenuItemIngredients(int menuItemId, [FromBody] List<MenuItemIngredientCreateDto> dtos)
    {
        Validation.ValidateRequest(menuItemId, dtos, nameof(menuItemId), nameof(dtos));
        _logger.LogInformation("AddMenuItemIngredients requested for MenuItem ID: {MenuItemId}", menuItemId);
        var created = await _itemService.AddMenuItemIngredientsAsync(menuItemId, dtos);
        return Created("", created);
    }

    [HttpPut("menu-items/{menuItemId:int}/ingredients")]
    public async Task<ActionResult<IEnumerable<MenuItemIngredientDto>>> UpdateMenuItemIngredients(int menuItemId, [FromBody] List<MenuItemIngredientCreateDto> dtos)
    {
        Validation.ValidateRequest(menuItemId, dtos, nameof(menuItemId), nameof(dtos));
        _logger.LogInformation("UpdateMenuItemIngredients requested for MenuItem ID: {MenuItemId}", menuItemId);
        var updated = await _itemService.UpdateMenuItemIngredientsAsync(menuItemId, dtos);
        return Ok(updated);
    }

    [HttpDelete("menu-items/{menuItemId:int}/ingredients/{ingredientId:int}")]
    public async Task<ActionResult> RemoveMenuItemIngredient(int menuItemId, int ingredientId)
    {
        Validation.ValidateId(menuItemId, nameof(menuItemId));
        Validation.ValidateId(ingredientId, nameof(ingredientId));
        _logger.LogInformation("RemoveMenuItemIngredient requested for MenuItem ID: {MenuItemId}, Ingredient ID: {IngredientId}", menuItemId, ingredientId);
        await _itemService.RemoveMenuItemIngredientAsync(menuItemId, ingredientId);
        return NoContent();
    }
}
