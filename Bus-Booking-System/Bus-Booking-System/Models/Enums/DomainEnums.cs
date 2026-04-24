namespace Bus_Booking_System.Models.Enums;

public enum UserRole
{
    User,
    Operator,
    Admin
}

public enum OperatorStatus
{
    Pending,
    Approved,
    Rejected,
    Suspended
}

public enum LocationStatus
{
    Pending,
    Approved,
    Rejected
}

public enum RouteStatus
{
    Active,
    Inactive
}

public enum BusStatus
{
    Pending,
    Approved,
    Rejected,
    Inactive
}

public enum TripStatus
{
    Scheduled,
    Active,
    Completed,
    Cancelled
}

public enum SeatBookingStatus
{
    Reserved,
    Confirmed,
    Cancelled
}

public enum PassengerGender
{
    Male,
    Female,
    Other
}

public enum TicketStatus
{
    Pending,
    Confirmed,
    Cancelled
}

public enum PaymentStatus
{
    Pending,
    Success,
    Failed
}