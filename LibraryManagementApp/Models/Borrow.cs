using LibraryManagementApp.Enums;

namespace LibraryManagementApp.Models;

internal class Borrow
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public Member User { get; set; } = null!;

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;

    public int BookCopyId { get; set; }
    public BookCopy BookCopy { get; set; } = null!;

    public DateTime BorrowDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }

    public BorrowStatus Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}