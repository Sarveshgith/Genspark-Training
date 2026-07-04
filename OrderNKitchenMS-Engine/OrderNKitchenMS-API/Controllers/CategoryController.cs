using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Services.Interfaces;
using Microsoft.Extensions.Logging;
using OrderNKitchenMS_API.Utils;

namespace OrderNKitchenMS_API.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories([FromQuery] bool? isNonVeg = null)
    {
        _logger.LogInformation("GetCategories requested. isNonVeg filter: {IsNonVeg}", isNonVeg);
        var categories = await _categoryService.GetAllAsync(isNonVeg);
        return Ok(categories);
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryDto>> GetCategoryById(int id)
    {
        Validation.ValidateId(id);
        _logger.LogInformation("GetCategoryById requested for ID: {Id}", id);
        var category = await _categoryService.GetByIdAsync(id);
        return Ok(category);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CategoryCreateDto categoryCreateDto)
    {
        Validation.RequireNotNull(categoryCreateDto, nameof(categoryCreateDto));
        _logger.LogInformation("CreateCategory requested for Category: {Name}", categoryCreateDto.Name);
        var createdCategory = await _categoryService.CreateAsync(categoryCreateDto);
        return CreatedAtAction(nameof(GetCategoryById), new { id = createdCategory.Id }, createdCategory);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, [FromBody] CategoryUpdateDto categoryUpdateDto)
    {
        Validation.ValidateRequest(id, categoryUpdateDto);
        _logger.LogInformation("UpdateCategory requested for ID: {Id}", id);
        var updatedCategory = await _categoryService.UpdateAsync(id, categoryUpdateDto);
        return Ok(updatedCategory);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteCategory(int id)
    {
        Validation.ValidateId(id);
        _logger.LogInformation("DeleteCategory requested for ID: {Id}", id);
        await _categoryService.DeleteAsync(id);
        return NoContent();
    }
}