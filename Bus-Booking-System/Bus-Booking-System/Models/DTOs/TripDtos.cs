using Bus_Booking_System.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bus_Booking_System.Models.DTOs;

public sealed class CreateTripRequest
{
    [Required]
    public Guid BusId { get; set; }

    public Guid? RouteId { get; set; }

    public Guid? FromId { get; set; }

    public Guid? ToId { get; set; }

    public TripStatus Status { get; set; } = TripStatus.Scheduled;

    [Required]
    public DateTime DepartureTime { get; set; }

    [Required]
    public DateTime ArrivalTime { get; set; }

    [Required]
    public decimal PricePerSeat { get; set; }
}

public sealed class UpdateTripRequest
{
    [Required]
    public TripStatus Status { get; set; } = TripStatus.Scheduled;

    [Required]
    public DateTime DepartureTime { get; set; }

    [Required]
    public DateTime ArrivalTime { get; set; }

    [Required]
    public decimal PricePerSeat { get; set; }
}
