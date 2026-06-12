using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Utils;

namespace OrderNKitchenMS_API.Controllers;

[ApiController]
[Route("api/menu")]
[Authorize]
public class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenuController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MenuItemDto>>> GetMenuItems([FromQuery] QueryMenuItemDto query)
    {
        var menuItems = await _menuService.GetAllAsync(query ?? new QueryMenuItemDto());
        return Ok(menuItems);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MenuItemDto>> GetMenuItemById(int id)
    {
        var menuItem = await _menuService.GetByIdAsync(id);
        return Ok(menuItem);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<ActionResult<MenuItemDto>> CreateMenuItem([FromBody] MenuItemCreateDto menuItemCreateDto)
    {
        var createdMenuItem = await _menuService.CreateAsync(menuItemCreateDto);
        return CreatedAtAction(nameof(GetMenuItemById), new { id = createdMenuItem.Id }, createdMenuItem);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<MenuItemDto>> UpdateMenuItem(int id, [FromBody] MenuItemUpdateDto menuItemUpdateDto)
    {
        var updatedMenuItem = await _menuService.UpdateAsync(id, menuItemUpdateDto);
        return Ok(updatedMenuItem);
    }

    [Authorize(Policy = "AdminOrChef")]
    [HttpPatch("{id:int}/availability")]
    public async Task<ActionResult> ToggleAvailability(int id, [FromBody] MenuItemAvailabilityDto payload)
    {
        Validation.RequireNotNull(payload, nameof(payload), "Availability payload is required.");
        await _menuService.ToggleAvailabilityAsync(id, payload.IsAvailable);
        return NoContent();
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteMenuItem(int id)
    {
        await _menuService.DeleteAsync(id);
        return NoContent();
    }
}
