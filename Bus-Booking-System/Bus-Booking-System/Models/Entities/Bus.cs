using Bus_Booking_System.Models.Enums;

namespace Bus_Booking_System.Models.Entities;

public class Bus
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OperatorId { get; set; }
    public Guid LayoutId { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    public BusStatus Status { get; set; } = BusStatus.Pending;
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Operator Operator { get; set; } = null!;
    public BusLayout Layout { get; set; } = null!;
    public User? ApprovedByUser { get; set; }
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
