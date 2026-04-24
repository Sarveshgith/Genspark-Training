using System.Text.Json;

namespace Bus_Booking_System.Models.Entities;

public class BusLayout
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int TotalSeats { get; set; }
    public JsonDocument Config { get; set; } = JsonDocument.Parse("{}");
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Bus> Buses { get; set; } = new List<Bus>();
}
