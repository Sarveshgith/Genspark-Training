using System;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagementAPI.Models.DTOs;

public class BookDTO
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public int PublicationYear { get; set; }
    public int AvailableCopies { get; set; }
}

public class CreateBookDTO
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Author { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[0-9Xx-]+$", ErrorMessage = "ISBN can contain only digits, hyphens, and X.")]
    public string ISBN { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int PublicationYear { get; set; }

    [Range(0, int.MaxValue)]
    public int AvailableCopies { get; set; }
}

public class UpdateBookDTO
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Author { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^[0-9Xx-]+$", ErrorMessage = "ISBN can contain only digits, hyphens, and X.")]
    public string ISBN { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int PublicationYear { get; set; }

    [Range(0, int.MaxValue)]
    public int AvailableCopies { get; set; }
}