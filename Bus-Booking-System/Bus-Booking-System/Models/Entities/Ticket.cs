using Bus_Booking_System.Models.Enums;

namespace Bus_Booking_System.Models.Entities;

public class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BookingRef { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public Guid TripId { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Pending;
    public decimal BaseAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? PaymentRef { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Trip Trip { get; set; } = null!;
    public ICollection<SeatBooking> SeatBookings { get; set; } = new List<SeatBooking>();
}
