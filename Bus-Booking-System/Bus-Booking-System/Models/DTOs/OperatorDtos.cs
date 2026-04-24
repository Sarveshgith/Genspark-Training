using Bus_Booking_System.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bus_Booking_System.Models.DTOs;

public sealed class UpdateOperatorStatusRequest
{
    [Required]
    public OperatorStatus Status { get; set; }
}

public sealed class UpdateOperatorLocationStatusRequest
{
    public bool IsActive { get; set; }
}

public sealed class AddOperatorLocationRequest
{
    [Required]
    public Guid LocationId { get; set; }

    [Required]
    public string Address { get; set; } = string.Empty;
}

public sealed class RequestLocationApprovalRequest
{
    [Required]
    public string City { get; set; } = string.Empty;

    [Required]
    public string State { get; set; } = string.Empty;
}
