using System.ComponentModel.DataAnnotations;

namespace OrderNKitchenMS_API.Models.Entities;

public class MenuItem : BaseEntity
{
    public int Id {get; set;}

    [Required, MaxLength(150)]
    public required string Name {get; set;}

    [MaxLength(500)]
    public string Description {get; set;} = string.Empty;

    [Required, Range(0.01, 10000)]
    public required decimal Price {get; set;}

    [Required]
    public int CategoryId {get; set;}
    public Category Category {get; set;} = null!;

    [Url, MaxLength(250)]
    public string ImageUrl {get; set;} = string.Empty;

    [Range(0, 600)]
    public required int PreparationTime {get; set;} // in minutes

    //Temporary Removal
    public bool IsAvailable {get; set;}

    public bool IsManuallyDisabled {get; set;} = false;

    //Permanent Removal
    public bool IsDeleted {get; set;}
}

