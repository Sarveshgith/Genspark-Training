using Bus_Booking_System.Models.Enums;

namespace Bus_Booking_System.Models.Entities;

public class Trip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusId { get; set; }
    public Guid RouteId { get; set; }
    public TripStatus Status { get; set; } = TripStatus.Scheduled;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public decimal PricePerSeat { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Bus Bus { get; set; } = null!;
    public Route Route { get; set; } = null!;
    public ICollection<SeatBooking> SeatBookings { get; set; } = new List<SeatBooking>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
