using LibraryManagementApp.Enums;

namespace LibraryManagementApp.Models;

public class Membership
{
    public int Id { get; set; }
    public MembershipType Type { get; set; }
    public int MaxBrwDays { get; set; }
    public int MaxBrwBooks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
