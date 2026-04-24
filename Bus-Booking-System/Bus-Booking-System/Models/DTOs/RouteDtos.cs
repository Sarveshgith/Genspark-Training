using Bus_Booking_System.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bus_Booking_System.Models.DTOs;

public sealed class CreateRouteRequest
{
    [Required]
    public Guid FromId { get; set; }

    [Required]
    public Guid ToId { get; set; }

    public RouteStatus Status { get; set; } = RouteStatus.Active;
}

public sealed class UpdateRouteRequest
{
    [Required]
    public RouteStatus Status { get; set; } = RouteStatus.Active;
}
