using System;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementAPI.Models.DTOs;

public class MemberDTO
{
    public int MemberId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNo { get; set; } = string.Empty;

    public DateTime MembershipDate { get; set; }
}

public class CreateMemberDTO
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Phone number must be 10 digits.")]
    public string PhoneNo { get; set; } = string.Empty;

    [Required]
    public DateTime MembershipDate { get; set; }
}