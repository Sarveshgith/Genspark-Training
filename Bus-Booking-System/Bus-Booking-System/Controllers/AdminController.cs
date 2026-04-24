using Bus_Booking_System.Data;
using Bus_Booking_System.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bus_Booking_System.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class AdminController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("pending/operators")]
    public async Task<IActionResult> PendingOperators()
    {
        var rows = await dbContext.Operators
            .Include(x => x.User)
            .Where(x => x.Status == OperatorStatus.Pending)
            .Select(x => new
            {
                x.UserId,
                x.LicenseNumber,
                Status = x.Status.ToString(),
                x.CreatedAt,
                User = new
                {
                    x.User.Name,
                    x.User.Email,
                    x.User.Phone
                }
            })
            .ToListAsync();

        return Ok(rows);
    }

    [HttpGet("pending/buses")]
    public async Task<IActionResult> PendingBuses()
    {
        var rows = await dbContext.Buses
            .Where(x => x.Status == BusStatus.Pending)
            .Select(x => new
            {
                x.Id,
                x.OperatorId,
                x.LayoutId,
                x.VehicleNumber,
                Status = x.Status.ToString(),
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(rows);
    }

    [HttpGet("dashboard/metrics")]
    public async Task<IActionResult> Metrics()
    {
        var metrics = new
        {
            Users = await dbContext.Users.CountAsync(),
            Operators = await dbContext.Operators.CountAsync(),
            Buses = await dbContext.Buses.CountAsync(),
            Trips = await dbContext.Trips.CountAsync(),
            Tickets = await dbContext.Tickets.CountAsync(),
            SeatBookings = await dbContext.SeatBookings.CountAsync(),
            ConfirmedTickets = await dbContext.Tickets.CountAsync(x => x.Status == TicketStatus.Confirmed),
            CancelledTickets = await dbContext.Tickets.CountAsync(x => x.Status == TicketStatus.Cancelled)
        };

        return Ok(metrics);
    }
}
