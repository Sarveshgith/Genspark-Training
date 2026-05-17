namespace LibraryManagementApp.Models;

internal class Book
{
    public int Id { get; set; }
    public string ISBN { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    //Navigation property => Easy to access obj without quering again
    //Acts as default JOIN in SQL
    public Category Category { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}