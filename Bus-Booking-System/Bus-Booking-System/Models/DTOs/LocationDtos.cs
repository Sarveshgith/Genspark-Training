using Bus_Booking_System.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bus_Booking_System.Models.DTOs;

public sealed class CreateLocationRequest
{
    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string State { get; set; } = string.Empty;

    public LocationStatus Status { get; set; } = LocationStatus.Pending;
}

public sealed class UpdateLocationRequest
{
    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string State { get; set; } = string.Empty;

    public LocationStatus Status { get; set; } = LocationStatus.Pending;
}
