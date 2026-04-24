namespace Bus_Booking_System.Models.Entities;

public class OperatorLocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OperatorId { get; set; }
    public Guid LocationId { get; set; }
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Operator Operator { get; set; } = null!;
    public Location Location { get; set; } = null!;
}
