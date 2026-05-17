namespace LibraryManagementApp.Models;

internal class Category
{
    public int Id { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}