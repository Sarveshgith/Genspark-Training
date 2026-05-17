using LibraryManagementApp.Enums;

namespace LibraryManagementApp.Models;

internal class Member
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PhoneNo { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public MemberStatus Status { get; set; }

    public int MembershipId { get; set; }
    public Membership Membership { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}