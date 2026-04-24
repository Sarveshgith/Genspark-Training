using Bus_Booking_System.Models.Enums;

namespace Bus_Booking_System.Models.Entities;

public class SeatBooking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public Guid UserId { get; set; }
    public Guid TripId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string? PassengerName { get; set; }
    public int? PassengerAge { get; set; }
    public PassengerGender? PassengerGender { get; set; }
    public SeatBookingStatus Status { get; set; } = SeatBookingStatus.Reserved;
    public DateTime? ReservedUntil { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Ticket Ticket { get; set; } = null!;
    public User User { get; set; } = null!;
    public Trip Trip { get; set; } = null!;
}
