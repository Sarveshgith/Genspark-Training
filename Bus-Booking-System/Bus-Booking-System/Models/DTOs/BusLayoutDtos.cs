using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Bus_Booking_System.Models.DTOs;

public sealed class CreateBusLayoutRequest
{
    [Required]
    public int TotalSeats { get; set; }

    [Required]
    public JsonElement Config { get; set; }
}

public sealed class UpdateBusLayoutRequest
{
    [Required]
    public int TotalSeats { get; set; }

    [Required]
    public JsonElement Config { get; set; }
}
