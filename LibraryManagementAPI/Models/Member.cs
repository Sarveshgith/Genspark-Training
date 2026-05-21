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
    public string FullName {get; set;} = string.Empty;

    [Required]
    [EmailAddress]
    public string Email {get; set;} = string.Empty;

    [Required]
    public string PhoneNo {get; set;} = string.Empty;

    [Required]
    public DateTime MembershipDate {get; set;}
}
