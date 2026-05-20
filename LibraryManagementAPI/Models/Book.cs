using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementAPI.Models;

//Indexing ISBN for faster search and enforcing uniqueness
[Index(nameof(ISBN), IsUnique = true)]
public class Book
{
    public int BookId {get; set;}

    [Required]
    public string Title {get; set;} = string.Empty;

    [Required]
    public string Author {get; set;} = string.Empty;

    //Example ISBN formats: "978-3-16-148410-0", "0-306-40615-2", "123456789X"
    [Required]
    [RegularExpression(@"^[0-9Xx-]+$", ErrorMessage = "ISBN can contain only digits, hyphens, and X.")]
    public string ISBN {get; set;} = string.Empty;

    public int PublicationYear {get; set;}

    [Range(0, int.MaxValue)]
    public int AvailableCopies {get; set;}
}
