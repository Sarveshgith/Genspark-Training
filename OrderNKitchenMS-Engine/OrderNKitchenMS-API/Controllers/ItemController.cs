using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Utils;

namespace OrderNKitchenMS_API.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Policy = "AdminOrChef")]
public class ItemController : ControllerBase
{
    private readonly IItemService _itemService;

    public ItemController(IItemService itemService)
    {
        _itemService = itemService;
    }

    [HttpGet("items")]
    public async Task<ActionResult<IEnumerable<ItemDto>>> GetItems()
    {
        var items = await _itemService.GetAllItemsAsync();
        return Ok(items);
    }

    [HttpGet("items/{id:int}")]
    public async Task<ActionResult<ItemDto>> GetItemById(int id)
    {
        var item = await _itemService.GetItemByIdAsync(id);
        return Ok(item);
    }

    [HttpGet("items/low-stock")]
    public async Task<ActionResult<IEnumerable<ItemDto>>> GetLowStock()
    {
        var lowStock = await _itemService.GetLowStockItemsAsync();
        return Ok(lowStock);
    }

    [HttpPost("items")]
    public async Task<ActionResult<ItemDto>> CreateItem([FromBody] ItemCreateDto dto)
    {
        Validation.RequireNotNull(dto, nameof(dto), "Item creation payload is required.");
        var created = await _itemService.CreateItemAsync(dto);
        return CreatedAtAction(nameof(GetItemById), new { id = created.Id }, created);
    }

    [HttpPut("items/{id:int}")]
    public async Task<ActionResult<ItemDto>> UpdateItem(int id, [FromBody] ItemUpdateDto dto)
    {
        Validation.RequireNotNull(dto, nameof(dto), "Item update payload is required.");
        var updated = await _itemService.UpdateItemAsync(id, dto);
        return Ok(updated);
    }

    [HttpPatch("items/{id:int}/restock")]
    public async Task<ActionResult<ItemDto>> RestockItem(int id, [FromBody] ItemRestockDto dto)
    {
        Validation.RequireNotNull(dto, nameof(dto), "Item restock payload is required.");
        var updated = await _itemService.RestockItemAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("items/{id:int}")]
    public async Task<ActionResult> DeleteItem(int id)
    {
        await _itemService.ChangeItemStatusAsync(id, false);
        return NoContent();
    }

    [HttpGet("menu-items/{menuItemId:int}/ingredients")]
    public async Task<ActionResult<IEnumerable<MenuItemIngredientDto>>> GetIngredientsByMenuItemId(int menuItemId)
    {
        var ingredients = await _itemService.GetIngredientsByMenuItemIdAsync(menuItemId);
        return Ok(ingredients);
    }

    [HttpPost("menu-items/{menuItemId:int}/ingredients")]
    public async Task<ActionResult<IEnumerable<MenuItemIngredientDto>>> AddMenuItemIngredients(int menuItemId, [FromBody] List<MenuItemIngredientCreateDto> dtos)
    {
        Validation.RequireNotNull(dtos, nameof(dtos), "Ingredient mappings list is required.");
        var created = await _itemService.AddMenuItemIngredientsAsync(menuItemId, dtos);
        return Created("", created);
    }

    [HttpPut("menu-items/{menuItemId:int}/ingredients")]
    public async Task<ActionResult<IEnumerable<MenuItemIngredientDto>>> UpdateMenuItemIngredients(int menuItemId, [FromBody] List<MenuItemIngredientCreateDto> dtos)
    {
        Validation.RequireNotNull(dtos, nameof(dtos), "Ingredient mappings list is required.");
        var updated = await _itemService.UpdateMenuItemIngredientsAsync(menuItemId, dtos);
        return Ok(updated);
    }

    [HttpDelete("menu-items/{menuItemId:int}/ingredients/{ingredientId:int}")]
    public async Task<ActionResult> RemoveMenuItemIngredient(int menuItemId, int ingredientId)
    {
        await _itemService.RemoveMenuItemIngredientAsync(menuItemId, ingredientId);
        return NoContent();
    }
}
