namespace LibraryManagementApp.Models;

internal class Fine
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public Member User { get; set; } = null!;

    public int BorrowId { get; set; }
    public Borrow Borrow { get; set; } = null!;

    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}