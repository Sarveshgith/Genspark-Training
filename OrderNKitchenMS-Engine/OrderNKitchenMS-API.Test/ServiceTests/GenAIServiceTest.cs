using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using OrderNKitchenMS_API.Services;
using OrderNKitchenMS_API.Services.Interfaces;

namespace OrderNKitchenMS_API.Test.ServiceTests;

[TestFixture]
public class GenAIServiceTest
{
    private IConfiguration _configuration = null!;
    private Mock<ILogger<GenAIService>> _loggerMock = null!;
    private IGenAIService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _configuration = new ConfigurationBuilder().Build();
        _loggerMock = new Mock<ILogger<GenAIService>>();
        _service = new GenAIService(_configuration, _loggerMock.Object);

        // Ensure API key environment variable is not set to force fallback execution path
        Environment.SetEnvironmentVariable("GEMINI_API_KEY", null);
    }

    [TestCase("Wagyu Steak", "premium, marbled beef seared to absolute perfection")]
    [TestCase("Beef Tenderloin", "marbled beef seared to absolute perfection")]
    [TestCase("Garden Salad", "crisp textures and vibrant seasonal ingredients")]
    [TestCase("Arugula Beet Salad", "crisp textures and vibrant seasonal ingredients")]
    [TestCase("Lemon Spritz", "botanical fusion designed to revive your senses")]
    [TestCase("Mocktail Drink", "botanical fusion designed to revive your senses")]
    [TestCase("Club Sandwich", "layers premium artisanal bread with perfectly seasoned proteins")]
    [TestCase("Classic Cheeseburger", "layers premium artisanal bread with perfectly seasoned proteins")]
    [TestCase("Classic Carbonara Pasta", "carefully curated with pristine ingredients and balanced textures")]
    public async Task GetDishStoryAsync_FallbackPaths_ReturnsExpectedStories(string dishName, string expectedSnippet)
    {
        // Act
        var result = await _service.GetDishStoryAsync(dishName);

        // Assert
        Assert.That(result, Contains.Substring(expectedSnippet));
    }

    [Test]
    public async Task GetDishStoryAsync_ApiKeyPresentButFails_ReturnsFallbackStory()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GEMINI_API_KEY", "dummy-api-key");
        var service = new GenAIService(_configuration, _loggerMock.Object);

        // Act
        var result = await service.GetDishStoryAsync("Wagyu Steak");

        // Assert
        Assert.That(result, Contains.Substring("premium, marbled beef seared to absolute perfection"));
        
        // Cleanup
        Environment.SetEnvironmentVariable("GEMINI_API_KEY", null);
    }
}
