using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Utils;
using Microsoft.Extensions.Logging;

namespace OrderNKitchenMS_API.Controllers;

[ApiController]
[Route("api/menu")]
[Authorize]
public class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;
    private readonly ILogger<MenuController> _logger;

    public MenuController(IMenuService menuService, ILogger<MenuController> logger)
    {
        _menuService = menuService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MenuItemDto>>> GetMenuItems([FromQuery] QueryMenuItemDto query)
    {
        _logger.LogInformation("GetMenuItems requested.");
        var menuItems = await _menuService.GetAllAsync(query ?? new QueryMenuItemDto());
        return Ok(menuItems);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MenuItemDto>> GetMenuItemById(int id)
    {
        Validation.ValidateId(id);
        _logger.LogInformation("GetMenuItemById requested for ID: {Id}", id);
        var menuItem = await _menuService.GetByIdAsync(id);
        return Ok(menuItem);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<ActionResult<MenuItemDto>> CreateMenuItem([FromBody] MenuItemCreateDto menuItemCreateDto)
    {
        Validation.RequireNotNull(menuItemCreateDto, nameof(menuItemCreateDto));
        _logger.LogInformation("CreateMenuItem requested for Menu Item: '{Name}'", menuItemCreateDto.Name);
        var createdMenuItem = await _menuService.CreateAsync(menuItemCreateDto);
        return CreatedAtAction(nameof(GetMenuItemById), new { id = createdMenuItem.Id }, createdMenuItem);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<MenuItemDto>> UpdateMenuItem(int id, [FromBody] MenuItemUpdateDto menuItemUpdateDto)
    {
        Validation.ValidateRequest(id, menuItemUpdateDto);
        _logger.LogInformation("UpdateMenuItem requested for ID: {Id}", id);
        var updatedMenuItem = await _menuService.UpdateAsync(id, menuItemUpdateDto);
        return Ok(updatedMenuItem);
    }

    [Authorize(Policy = "AdminOrChef")]
    [HttpPatch("{id:int}/availability")]
    public async Task<ActionResult> ToggleAvailability(int id, [FromBody] MenuItemAvailabilityDto payload)
    {
        Validation.ValidateRequest(id, payload);
        _logger.LogInformation("ToggleAvailability requested for ID: {Id}, IsAvailable: {IsAvailable}", id, payload.IsAvailable);
        await _menuService.ToggleAvailabilityAsync(id, payload.IsAvailable);
        return NoContent();
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteMenuItem(int id)
    {
        Validation.ValidateId(id);
        _logger.LogInformation("DeleteMenuItem requested for ID: {Id}", id);
        await _menuService.DeleteAsync(id);
        return NoContent();
    }
}
