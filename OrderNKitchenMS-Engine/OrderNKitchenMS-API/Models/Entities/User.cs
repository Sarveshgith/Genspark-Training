using System;
using System.ComponentModel.DataAnnotations;

namespace OrderNKitchenMS_API.Models.Entities;

public class User : BaseEntity
{
    public int Id {get; set;}

    [Required, MaxLength(100)]
    public required string Name {get; set;}

    [Required, EmailAddress, MaxLength(150)]
    public required string Email{get; set;}

    [Required, MaxLength(256)]
    public required string PasswordHash {get; set;}

    [MaxLength(10)]
    public string? PhoneNumber {get; set;}

    [MaxLength(250)]
    public string? Address {get; set;}

    [Required]
    public int RoleId {get; set;}
    public Role? Role {get; set;}

    public bool IsDeleted {get; set;}
    public bool IsPending {get; set;} = true;
}
