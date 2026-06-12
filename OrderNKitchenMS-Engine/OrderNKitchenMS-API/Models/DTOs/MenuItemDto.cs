using System;

namespace OrderNKitchenMS_API.Models.DTOs;

public class MenuItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int PreparationTime { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsManuallyDisabled { get; set; }
    public DateTime CreatedAt { get; set; }

}

public class QueryMenuItemDto
{
    public string? Name {get; set;} = string.Empty;
    public int? CategoryId {get; set;}
    public decimal? MinPrice {get; set;}
    public decimal? MaxPrice {get; set;}
    public bool? IsAvailable {get; set;}
    public int? MaxPreparationTime {get; set;}
    public int PageNumber {get; set;} = 1;
    public int PageSize {get; set;} = 10;
}

public class MenuItemCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int PreparationTime { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public class MenuItemUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CategoryId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int PreparationTime { get; set; }
    public bool IsAvailable { get; set; }
}

public class MenuItemAvailabilityDto
{
    public bool IsAvailable { get; set; }
}
