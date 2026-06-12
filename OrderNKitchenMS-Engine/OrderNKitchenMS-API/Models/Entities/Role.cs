using System.ComponentModel.DataAnnotations;
using OrderNKitchenMS_API.Models.Enums;

namespace OrderNKitchenMS_API.Models.Entities;

public class Role : BaseEntity
{
    public int Id {get; set;}

    [Required]
    public required UserRole Name {get; set;}

    public ICollection<User> Users {get; set;} = new List<User>();
}
