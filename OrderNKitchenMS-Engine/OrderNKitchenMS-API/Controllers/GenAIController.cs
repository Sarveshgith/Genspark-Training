using Microsoft.AspNetCore.Mvc;
using OrderNKitchenMS_API.Services;

namespace OrderNKitchenMS_API.Controllers;

[ApiController]
[Route("api/genai")]
public class GenAIController : ControllerBase
{
    private readonly GenAIService _genAIService;

    public GenAIController(GenAIService genAIService)
    {
        _genAIService = genAIService;
    }

    [HttpGet("dish-story")]
    public async Task<ActionResult<string>> GetDishStory([FromQuery] string dishName)
    {
        if (string.IsNullOrWhiteSpace(dishName))
        {
            return BadRequest("Dish name is required.");
        }

        var story = await _genAIService.GetDishStoryAsync(dishName);
        return Ok(story);
    }
}