using Bus_Booking_System.Models.Enums;

namespace Bus_Booking_System.Models.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Operator? OperatorProfile { get; set; }
    public ICollection<Operator> ApprovedOperators { get; set; } = new List<Operator>();
    public ICollection<Bus> ApprovedBuses { get; set; } = new List<Bus>();
    public ICollection<Route> CreatedRoutes { get; set; } = new List<Route>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<SeatBooking> SeatBookings { get; set; } = new List<SeatBooking>();
}
