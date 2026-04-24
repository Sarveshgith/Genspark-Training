using Bus_Booking_System.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bus_Booking_System.Models.DTOs;

public sealed class CreateBusRequest
{
    [Required]
    public Guid OperatorId { get; set; }

    [Required]
    public Guid LayoutId { get; set; }

    [Required]
    [MaxLength(20)]
    public string VehicleNumber { get; set; } = string.Empty;

    public BusStatus Status { get; set; } = BusStatus.Pending;
}

public sealed class UpdateBusRequest
{
    [Required]
    public Guid LayoutId { get; set; }

    [Required]
    [MaxLength(20)]
    public string VehicleNumber { get; set; } = string.Empty;

    public BusStatus Status { get; set; } = BusStatus.Pending;
}

public sealed class ApproveBusRequest
{
    [Required]
    public BusStatus Status { get; set; } = BusStatus.Approved;
}
