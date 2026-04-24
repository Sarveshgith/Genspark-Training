using Bus_Booking_System.Models.Enums;

namespace Bus_Booking_System.Models.Entities;

public class Route
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FromId { get; set; }
    public Guid ToId { get; set; }
    public RouteStatus Status { get; set; } = RouteStatus.Active;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Location From { get; set; } = null!;
    public Location To { get; set; } = null!;
    public User? CreatedByUser { get; set; }
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
