// @feature Backend API | GenAI Service Endpoint | Integrates with Gemini AI to generate stories, facts, and trivia regarding specific dishes.
using Microsoft.AspNetCore.Mvc;
using OrderNKitchenMS_API.Services.Interfaces;

namespace OrderNKitchenMS_API.Controllers;

[ApiController]
[Route("api/genai")]
public class GenAIController : ControllerBase
{
    private readonly IGenAIService _genAIService;

    public GenAIController(IGenAIService genAIService)
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