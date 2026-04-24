using Bus_Booking_System.Models.Enums;

namespace Bus_Booking_System.Models.Entities;

public class Location
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public LocationStatus Status { get; set; } = LocationStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Route> FromRoutes { get; set; } = new List<Route>();
    public ICollection<Route> ToRoutes { get; set; } = new List<Route>();
    public ICollection<OperatorLocation> OperatorLocations { get; set; } = new List<OperatorLocation>();
}
