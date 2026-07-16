using Google.GenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderNKitchenMS_API.Services.Interfaces;

namespace OrderNKitchenMS_API.Services;

public class GenAIService : IGenAIService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GenAIService> _logger;
    private readonly string? _apiKey;

    public GenAIService(IConfiguration configuration, ILogger<GenAIService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _apiKey = _configuration["GEMINI_API_KEY"];
    }

    public async Task<string> GetDishStoryAsync(string dishName)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("GEMINI_API_KEY environment variable is not set. Utilizing local fallback story for '{DishName}'.", dishName);
            return GetLocalFallbackStory(dishName);
        }

        try
        {
            using var client = new Client(apiKey: _apiKey);
            var prompt = promptTemplate.Replace("{dishName}", dishName);
            var response = await client.Models.GenerateContentAsync(
                model: "gemini-3.1-flash-lite",
                contents: prompt
            );
            return response.Candidates[0].Content.Parts[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gemini API call failed for dish story of '{DishName}'. Utilizing local fallback story.", dishName);
            return GetLocalFallbackStory(dishName);
        }
    }

    private string GetLocalFallbackStory(string dishName)
    {
        var sanitized = dishName.ToLowerInvariant();
        if (sanitized.Contains("wagyu") || sanitized.Contains("steak") || sanitized.Contains("beef"))
        {
            return $"The {dishName} features premium, marbled beef seared to absolute perfection, locks in rich juices, and is paired with delicate savory herbs to elevate your dining journey.";
        }
        if (sanitized.Contains("salad") || sanitized.Contains("beet") || sanitized.Contains("veg"))
        {
            return $"Our garden-fresh {dishName} celebrates crisp textures and vibrant seasonal ingredients, brought together with a signature dressing for a light, refreshing palate cleanser.";
        }
        if (sanitized.Contains("spritz") || sanitized.Contains("drink") || sanitized.Contains("cocktail"))
        {
            return $"This refreshing {dishName} is a handcrafted botanical fusion designed to revive your senses and provide a bright, clean pairing between savory courses.";
        }
        if (sanitized.Contains("sandwich") || sanitized.Contains("burger"))
        {
            return $"The classic {dishName} layers premium artisanal bread with perfectly seasoned proteins and crisp toppings, creating a comforting harmony of savory warmth.";
        }
        return $"Our culinary team presents the {dishName}, carefully curated with pristine ingredients and balanced textures to create a harmonious and unforgettable flavor profile.";
    }

    private readonly string promptTemplate = 
        "You are a warm and highly knowledgeable culinary historian acting as a chef's assistant. " +
        "Your job is to write a short, captivating 2-to-3-sentence 'flavor tale' or origin story for the dish provided below. " +
        "CRITICAL FALLBACK RULES: " +
        "1. If the dish is a classic with a rich history, share its authentic origin story or a charming historical anecdote. " +
        "2. If the dish is modern or custom (e.g., 'Spicy Bacon Mac & Cheese'), DO NOT hallucinate a fake historical event. " +
        "Instead, pivot to an interesting culinary fact about its core ingredients or the flavor science. " +
        "3. Keep the tone mouth-watering and under 60 words. " +
        "4. Return STRICTLY plain text. Do not use Markdown, headers, bullet points, or quotation marks. " +
        "\n\nDish: {dishName}";
}
