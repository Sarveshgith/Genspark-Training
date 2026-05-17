using LibraryManagementApp.Enums;

namespace LibraryManagementApp.Models;

internal class BookCopy
{
    public int Id { get; set; }

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    public BookCopyStatus Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}