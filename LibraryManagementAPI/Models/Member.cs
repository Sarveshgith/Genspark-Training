using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementAPI.Models;

//Indexing Email and PhoneNo for faster lookups and enforcing uniqueness
[Index(nameof(Email), IsUnique = true)]
[Index(nameof(PhoneNo), IsUnique = true)]
public class Member
{
    public int MemberId {get; set;}

    [Required]
    //[StringLength(150, MinimumLength = 2)]
    public string FullName {get; set;} = string.Empty;

    [Required]
    [EmailAddress]
    public string Email {get; set;} = string.Empty;

    [Required]
    //[RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Phone number must contain only digits and be 10characters long.")]
    public string PhoneNo {get; set;} = string.Empty;

    [Required]
    public DateTime MembershipDate {get; set;}
}
