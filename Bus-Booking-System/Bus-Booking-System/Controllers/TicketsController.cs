using Bus_Booking_System.Data;
using Bus_Booking_System.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bus_Booking_System.Controllers;

[ApiController]
[Route("tickets")]
[Authorize]
public class TicketsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("{ticketId:guid}")]
    public async Task<IActionResult> GetByTicketId(Guid ticketId)
    {
        var ticket = await dbContext.Tickets
            .Include(x => x.SeatBookings)
            .FirstOrDefaultAsync(x => x.Id == ticketId);

        if (ticket is null)
        {
            return NotFound("Ticket not found.");
        }

        if (!await HasAccess(ticket.UserId, ticket.Id))
        {
            return Forbid();
        }

        return Ok(new
        {
            ticket.Id,
            ticket.BookingRef,
            ticket.UserId,
            ticket.TripId,
            Status = ticket.Status.ToString(),
            PaymentStatus = ticket.PaymentStatus.ToString(),
            ticket.BaseAmount,
            ticket.TotalAmount,
            ticket.PaymentRef,
            ticket.CreatedAt,
            Seats = ticket.SeatBookings.Select(x => new
            {
                x.SeatNumber,
                x.PassengerName,
                x.PassengerAge,
                PassengerGender = x.PassengerGender.HasValue ? x.PassengerGender.Value.ToString() : null,
                Status = x.Status.ToString()
            })
        });
    }

    [HttpGet("booking-ref/{bookingRef}")]
    public async Task<IActionResult> GetByBookingRef(string bookingRef)
    {
        var ticket = await dbContext.Tickets
            .Include(x => x.SeatBookings)
            .FirstOrDefaultAsync(x => x.BookingRef == bookingRef);

        if (ticket is null)
        {
            return NotFound("Ticket not found.");
        }

        if (!await HasAccess(ticket.UserId, ticket.Id))
        {
            return Forbid();
        }

        return Ok(new
        {
            ticket.Id,
            ticket.BookingRef,
            ticket.UserId,
            ticket.TripId,
            Status = ticket.Status.ToString(),
            PaymentStatus = ticket.PaymentStatus.ToString(),
            ticket.BaseAmount,
            ticket.TotalAmount,
            ticket.PaymentRef,
            ticket.CreatedAt,
            Seats = ticket.SeatBookings.Select(x => new
            {
                x.SeatNumber,
                x.PassengerName,
                x.PassengerAge,
                PassengerGender = x.PassengerGender.HasValue ? x.PassengerGender.Value.ToString() : null,
                Status = x.Status.ToString()
            })
        });
    }

    [HttpGet("{ticketId:guid}/print")]
    public async Task<IActionResult> Print(Guid ticketId)
    {
        var ticket = await dbContext.Tickets
            .Include(x => x.User)
            .Include(x => x.Trip)
            .ThenInclude(x => x.Route)
            .Include(x => x.SeatBookings)
            .FirstOrDefaultAsync(x => x.Id == ticketId);

        if (ticket is null)
        {
            return NotFound("Ticket not found.");
        }

        if (!await HasAccess(ticket.UserId, ticket.Id))
        {
            return Forbid();
        }

        return Ok(new
        {
            TicketNumber = ticket.BookingRef,
            Passenger = new
            {
                ticket.User.Name,
                ticket.User.Email,
                ticket.User.Phone
            },
            Journey = new
            {
                ticket.Trip.DepartureTime,
                ticket.Trip.ArrivalTime,
                Status = ticket.Trip.Status.ToString(),
                ticket.Trip.PricePerSeat
            },
            Fare = new
            {
                ticket.BaseAmount,
                ticket.TotalAmount,
                PaymentStatus = ticket.PaymentStatus.ToString(),
                ticket.PaymentRef
            },
            Seats = ticket.SeatBookings.Select(x => x.SeatNumber)
        });
    }

    private async Task<bool> HasAccess(Guid ownerUserId, Guid ticketId)
    {
        if (User.IsInRole(nameof(UserRole.Admin)))
        {
            return true;
        }

        var current = GetCurrentUserId();
        if (current == ownerUserId)
        {
            return true;
        }

        if (User.IsInRole(nameof(UserRole.Operator)) && current.HasValue)
        {
            return await dbContext.Tickets
                .Where(x => x.Id == ticketId)
                .Join(dbContext.Trips, t => t.TripId, tr => tr.Id, (t, tr) => new { t, tr })
                .Join(dbContext.Buses, x => x.tr.BusId, b => b.Id, (x, b) => new { x.t, BusOperatorId = b.OperatorId })
                .AnyAsync(x => x.BusOperatorId == current.Value);
        }

        return false;
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
