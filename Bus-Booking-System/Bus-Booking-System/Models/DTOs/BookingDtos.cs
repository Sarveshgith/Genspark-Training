using Bus_Booking_System.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bus_Booking_System.Models.DTOs;

public sealed class ReserveBookingRequest
{
    [Required]
    public Guid TripId { get; set; }

    [Required]
    public List<ReserveSeatItem> Seats { get; set; } = new();
}

public sealed class ReserveSeatItem
{
    [Required]
    [MaxLength(10)]
    public string SeatNumber { get; set; } = string.Empty;

    public string? PassengerName { get; set; }
    public int? PassengerAge { get; set; }
    public PassengerGender? PassengerGender { get; set; }
}

public sealed class ConfirmBookingRequest
{
    [Required]
    public Guid TicketId { get; set; }

    public string? PaymentRef { get; set; }
}

public sealed class PayBookingRequest
{
    [Required]
    public string PaymentRef { get; set; } = string.Empty;
}
