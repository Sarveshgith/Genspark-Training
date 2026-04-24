using Bus_Booking_System.Data;
using Bus_Booking_System.Models.DTOs;
using Bus_Booking_System.Models.Entities;
using Bus_Booking_System.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;

namespace Bus_Booking_System.Controllers;

[ApiController]
[Route("bookings")]
[Authorize]
public class BookingsController(AppDbContext dbContext, IConfiguration configuration, ILogger<BookingsController> logger) : ControllerBase
{
    [HttpPost("reserve")]
    public async Task<IActionResult> Reserve([FromBody] ReserveBookingRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var trip = await dbContext.Trips
            .Include(x => x.Bus)
            .Include(x => x.Route)
                .ThenInclude(x => x.From)
            .Include(x => x.Route)
                .ThenInclude(x => x.To)
            .FirstOrDefaultAsync(x => x.Id == request.TripId);
        if (trip is null)
        {
            return BadRequest("Invalid trip id.");
        }

        if (request.Seats.Count == 0)
        {
            return BadRequest("At least one seat is required.");
        }

        var baseAmount = trip.PricePerSeat * request.Seats.Count;
        var breakdown = BuildPaymentBreakdown(baseAmount);

        var ticket = new Ticket
        {
            BookingRef = GenerateBookingRef(),
            UserId = userId.Value,
            TripId = request.TripId,
            Status = TicketStatus.Pending,
            BaseAmount = baseAmount,
            TotalAmount = breakdown.Total,
            PaymentStatus = PaymentStatus.Pending
        };

        dbContext.Tickets.Add(ticket);

        foreach (var seat in request.Seats)
        {
            dbContext.SeatBookings.Add(new SeatBooking
            {
                TicketId = ticket.Id,
                UserId = userId.Value,
                TripId = request.TripId,
                SeatNumber = seat.SeatNumber.Trim(),
                PassengerName = seat.PassengerName,
                PassengerAge = seat.PassengerAge,
                PassengerGender = seat.PassengerGender,
                Status = SeatBookingStatus.Reserved,
                ReservedUntil = DateTime.UtcNow.AddMinutes(10)
            });
        }

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict("One or more seats are already taken for this trip.");
        }

        return Ok(new
        {
            ticket.Id,
            ticket.BookingRef,
            trip.BusId,
            Route = BuildRouteDisplay(trip.Route.From.City, trip.Route.From.State, trip.Route.To.City, trip.Route.To.State),
            ticket.BaseAmount,
            breakdown.ConvenienceFee,
            breakdown.ServiceFee,
            breakdown.TaxAmount,
            ticket.TotalAmount,
            PaymentStatus = ticket.PaymentStatus.ToString(),
            SeatCount = request.Seats.Count
        });
    }

    [HttpGet("{ticketId:guid}/payment-summary")]
    public async Task<IActionResult> GetPaymentSummary(Guid ticketId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var ticket = await dbContext.Tickets
            .Include(x => x.Trip)
                .ThenInclude(x => x.Bus)
            .Include(x => x.Trip)
                .ThenInclude(x => x.Route)
                    .ThenInclude(x => x.From)
            .Include(x => x.Trip)
                .ThenInclude(x => x.Route)
                    .ThenInclude(x => x.To)
            .Include(x => x.SeatBookings)
            .FirstOrDefaultAsync(x => x.Id == ticketId);

        if (ticket is null)
        {
            return NotFound("Ticket not found.");
        }

        if (!User.IsInRole(nameof(UserRole.Admin)) && ticket.UserId != userId.Value)
        {
            return Forbid();
        }

        var breakdown = BuildPaymentBreakdown(ticket.BaseAmount);

        return Ok(new
        {
            ticket.Id,
            ticket.BookingRef,
            ticket.BaseAmount,
            breakdown.ConvenienceFee,
            breakdown.ServiceFee,
            breakdown.TaxAmount,
            breakdown.Total,
            ticket.PaymentStatus,
            ticket.Status,
            ticket.Trip.BusId,
            Route = BuildRouteDisplay(
                ticket.Trip.Route.From.City,
                ticket.Trip.Route.From.State,
                ticket.Trip.Route.To.City,
                ticket.Trip.Route.To.State),
            Seats = ticket.SeatBookings.Select(x => x.SeatNumber).ToList()
        });
    }

    [HttpPost("{ticketId:guid}/pay")]
    public async Task<IActionResult> PayBooking(Guid ticketId, [FromBody] PayBookingRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var ticket = await dbContext.Tickets
            .Include(x => x.User)
            .Include(x => x.Trip)
                .ThenInclude(x => x.Bus)
            .Include(x => x.Trip)
                .ThenInclude(x => x.Route)
                    .ThenInclude(x => x.From)
            .Include(x => x.Trip)
                .ThenInclude(x => x.Route)
                    .ThenInclude(x => x.To)
            .Include(x => x.SeatBookings)
            .FirstOrDefaultAsync(x => x.Id == ticketId);

        if (ticket is null)
        {
            return NotFound("Ticket not found.");
        }

        if (!User.IsInRole(nameof(UserRole.Admin)) && ticket.UserId != userId.Value)
        {
            return Forbid();
        }

        if (ticket.PaymentStatus == PaymentStatus.Success)
        {
            return Conflict("Booking is already paid.");
        }

        if (ticket.Status == TicketStatus.Cancelled)
        {
            return BadRequest("Cancelled booking cannot be paid.");
        }

        var now = DateTime.UtcNow;
        var expiredSeat = ticket.SeatBookings.Any(x =>
            x.Status == SeatBookingStatus.Reserved &&
            x.ReservedUntil.HasValue &&
            x.ReservedUntil.Value < now);

        if (expiredSeat)
        {
            return Conflict("Reservation expired. Please book again.");
        }

        var breakdown = BuildPaymentBreakdown(ticket.BaseAmount);
        var emailSent = await TrySendBookingEmailAsync(ticket, breakdown);
        if (!emailSent)
        {
            return BadRequest("Payment not completed. Email could not be sent. Configure SMTP and retry.");
        }

        ticket.PaymentRef = request.PaymentRef.Trim();
        ticket.TotalAmount = breakdown.Total;
        ticket.PaymentStatus = PaymentStatus.Success;
        ticket.Status = TicketStatus.Confirmed;

        foreach (var seat in ticket.SeatBookings)
        {
            seat.Status = SeatBookingStatus.Confirmed;
            seat.ReservedUntil = null;
        }

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            ticket.Id,
            ticket.BookingRef,
            Status = ticket.Status.ToString(),
            PaymentStatus = ticket.PaymentStatus.ToString(),
            ticket.PaymentRef,
            ticket.TotalAmount
        });
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmBookingRequest request)
    {
        return BadRequest("Use POST /bookings/{ticketId}/pay to complete payment and booking.");
    }

    [HttpPost("release")]
    public async Task<IActionResult> ReleaseExpiredReservations()
    {
        var now = DateTime.UtcNow;

        var expiredSeats = await dbContext.SeatBookings
            .Where(x => x.Status == SeatBookingStatus.Reserved && x.ReservedUntil.HasValue && x.ReservedUntil < now)
            .ToListAsync();

        foreach (var seat in expiredSeats)
        {
            seat.Status = SeatBookingStatus.Cancelled;
            seat.ReservedUntil = null;
        }

        await dbContext.SaveChangesAsync();

        return Ok(new { ReleasedCount = expiredSeats.Count });
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyBookings()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var tickets = await dbContext.Tickets
            .Where(x => x.UserId == userId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.BookingRef,
                x.Trip.BusId,
                Route = x.Trip.Route.From.City + ", " + x.Trip.Route.From.State + " -> " + x.Trip.Route.To.City + ", " + x.Trip.Route.To.State,
                Status = x.Status.ToString(),
                PaymentStatus = x.PaymentStatus.ToString(),
                x.BaseAmount,
                x.TotalAmount,
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(tickets);
    }

    [HttpGet("{ticketId:guid}")]
    public async Task<IActionResult> GetBookingByTicketId(Guid ticketId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var ticket = await dbContext.Tickets
            .Include(x => x.Trip)
                .ThenInclude(x => x.Bus)
            .Include(x => x.Trip)
                .ThenInclude(x => x.Route)
                    .ThenInclude(x => x.From)
            .Include(x => x.Trip)
                .ThenInclude(x => x.Route)
                    .ThenInclude(x => x.To)
            .Include(x => x.SeatBookings)
            .FirstOrDefaultAsync(x => x.Id == ticketId);

        if (ticket is null)
        {
            return NotFound("Ticket not found.");
        }

        if (!User.IsInRole(nameof(UserRole.Admin)) && ticket.UserId != userId.Value)
        {
            return Forbid();
        }

        return Ok(new
        {
            ticket.Id,
            ticket.BookingRef,
            ticket.Trip.BusId,
            Route = BuildRouteDisplay(ticket.Trip.Route.From.City, ticket.Trip.Route.From.State, ticket.Trip.Route.To.City, ticket.Trip.Route.To.State),
            Status = ticket.Status.ToString(),
            PaymentStatus = ticket.PaymentStatus.ToString(),
            ticket.BaseAmount,
            ticket.TotalAmount,
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

    [HttpPost("{ticketId:guid}/cancel")]
    public async Task<IActionResult> CancelBooking(Guid ticketId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var ticket = await dbContext.Tickets
            .Include(x => x.SeatBookings)
            .FirstOrDefaultAsync(x => x.Id == ticketId);

        if (ticket is null)
        {
            return NotFound("Ticket not found.");
        }

        if (!User.IsInRole(nameof(UserRole.Admin)) && ticket.UserId != userId.Value)
        {
            return Forbid();
        }

        ticket.Status = TicketStatus.Cancelled;

        foreach (var seat in ticket.SeatBookings)
        {
            seat.Status = SeatBookingStatus.Cancelled;
            seat.ReservedUntil = null;
        }

        await dbContext.SaveChangesAsync();

        return Ok(new { ticket.Id, Status = ticket.Status.ToString() });
    }

    private static string GenerateBookingRef()
    {
        return $"BK{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
    }

    private static (decimal ConvenienceFee, decimal ServiceFee, decimal TaxAmount, decimal Total) BuildPaymentBreakdown(decimal baseAmount)
    {
        var convenience = Math.Round(baseAmount * 0.05m, 2, MidpointRounding.AwayFromZero);
        var service = 25m;
        var taxable = baseAmount + convenience + service;
        var tax = Math.Round(taxable * 0.18m, 2, MidpointRounding.AwayFromZero);
        var total = Math.Round(baseAmount + convenience + service + tax, 2, MidpointRounding.AwayFromZero);
        return (convenience, service, tax, total);
    }

    private static string BuildRouteDisplay(string fromCity, string fromState, string toCity, string toState)
    {
        return $"{fromCity}, {fromState} -> {toCity}, {toState}";
    }

    private async Task<bool> TrySendBookingEmailAsync(Ticket ticket, (decimal ConvenienceFee, decimal ServiceFee, decimal TaxAmount, decimal Total) breakdown)
    {
        var host = configuration["Smtp:Host"];
        var fromEmail = configuration["Smtp:FromEmail"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail))
        {
            return false;
        }

        var port = int.TryParse(configuration["Smtp:Port"], out var smtpPort) ? smtpPort : 587;
        var enableSsl = bool.TryParse(configuration["Smtp:EnableSsl"], out var ssl) ? ssl : true;
        var username = configuration["Smtp:Username"];
        var password = configuration["Smtp:Password"];

        var body = new StringBuilder()
            .AppendLine("Your booking has been confirmed.")
            .AppendLine($"Booking Ref: {ticket.BookingRef}")
            .AppendLine($"Bus ID: {ticket.Trip.BusId}")
            .AppendLine($"Route: {BuildRouteDisplay(ticket.Trip.Route.From.City, ticket.Trip.Route.From.State, ticket.Trip.Route.To.City, ticket.Trip.Route.To.State)}")
            .AppendLine($"Base Fare: {ticket.BaseAmount:0.00}")
            .AppendLine($"Convenience Fee: {breakdown.ConvenienceFee:0.00}")
            .AppendLine($"Service Fee: {breakdown.ServiceFee:0.00}")
            .AppendLine($"Tax: {breakdown.TaxAmount:0.00}")
            .AppendLine($"Total Paid: {breakdown.Total:0.00}")
            .ToString();

        try
        {
            using var message = new MailMessage(fromEmail, ticket.User.Email)
            {
                Subject = $"Bus Booking Confirmation - {ticket.BookingRef}",
                Body = body
            };

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl
            };

            if (!string.IsNullOrWhiteSpace(username))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            await client.SendMailAsync(message);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send booking confirmation email for ticket {TicketId}", ticket.Id);
            return false;
        }
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
