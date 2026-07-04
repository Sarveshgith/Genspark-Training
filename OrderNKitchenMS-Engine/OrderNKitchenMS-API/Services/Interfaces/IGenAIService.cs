using System.Threading.Tasks;

namespace OrderNKitchenMS_API.Services.Interfaces;

public interface IGenAIService
{
    Task<string> GetDishStoryAsync(string dishName);
}
