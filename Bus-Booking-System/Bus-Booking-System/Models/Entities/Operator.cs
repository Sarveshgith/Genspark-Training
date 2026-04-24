using Bus_Booking_System.Models.Enums;

namespace Bus_Booking_System.Models.Entities;

public class Operator
{
    public Guid UserId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public OperatorStatus Status { get; set; } = OperatorStatus.Pending;
    public Guid? ApprovedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public User? ApprovedByUser { get; set; }
    public ICollection<OperatorLocation> OperatorLocations { get; set; } = new List<OperatorLocation>();
    public ICollection<Bus> Buses { get; set; } = new List<Bus>();
}
