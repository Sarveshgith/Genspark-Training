using System.ComponentModel.DataAnnotations;

namespace OrderNKitchenMS_API.Models.Entities;

public class Category : BaseEntity
{
    public int Id {get; set;}

    [Required, MaxLength(100)]
    public required string Name {get; set;}

    public bool IsNonVeg {get; set;}

    public bool IsDeleted {get; set;}

    public ICollection<MenuItem> MenuItems {get; set;} = new List<MenuItem>();
}
