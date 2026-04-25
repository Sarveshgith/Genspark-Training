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
using RouteEntity = Bus_Booking_System.Models.Entities.Route;

namespace Bus_Booking_System.Controllers;

[ApiController]
[Route("trips")]
[Authorize]
public class TripsController(AppDbContext dbContext, IConfiguration configuration, ILogger<TripsController> logger) : ControllerBase
{
    // 🔥 CREATE TRIP
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Operator) + "," + nameof(UserRole.Admin))]
    public async Task<IActionResult> CreateTrip([FromBody] CreateTripRequest request)
    {
        var bus = await dbContext.Buses.FirstOrDefaultAsync(x => x.Id == request.BusId);
        if (bus is null)
            return BadRequest("Invalid bus id.");

        if (bus.Status != BusStatus.Approved)
            return BadRequest("Trip can be created only with approved bus.");

        if (!User.IsInRole(nameof(UserRole.Admin)) && bus.OperatorId != GetCurrentUserId())
            return Forbid();

        if (!TryNormalizeAndValidateTripTimes(request.DepartureTime, request.ArrivalTime, out var departureUtc, out var arrivalUtc, out var validationError))
            return BadRequest(validationError);

        Guid routeId;

        if (request.RouteId.HasValue)
        {
            var existingRoute = await dbContext.Routes
                .Include(x => x.From)
                .Include(x => x.To)
                .FirstOrDefaultAsync(x => x.Id == request.RouteId.Value);
            if (existingRoute is null)
                return BadRequest("Invalid route id.");

            if (existingRoute.From.Status != LocationStatus.Approved || existingRoute.To.Status != LocationStatus.Approved)
                return BadRequest("Route locations must be approved.");

            routeId = existingRoute.Id;
        }
        else
        {
            if (!request.FromId.HasValue || !request.ToId.HasValue)
                return BadRequest("Either routeId or fromId/toId is required.");

            if (request.FromId.Value == request.ToId.Value)
                return BadRequest("fromId and toId cannot be same.");

            var fromLocation = await dbContext.Locations.FirstOrDefaultAsync(x => x.Id == request.FromId.Value);
            var toLocation = await dbContext.Locations.FirstOrDefaultAsync(x => x.Id == request.ToId.Value);

            if (fromLocation is null || toLocation is null)
                return BadRequest("Invalid from/to location id.");

            if (fromLocation.Status != LocationStatus.Approved || toLocation.Status != LocationStatus.Approved)
                return BadRequest("Trip locations must be approved.");

            var route = await dbContext.Routes.FirstOrDefaultAsync(
                x => x.FromId == request.FromId.Value && x.ToId == request.ToId.Value);

            if (route is null)
            {
                route = new RouteEntity
                {
                    FromId = request.FromId.Value,
                    ToId = request.ToId.Value,
                    Status = RouteStatus.Active,
                    CreatedBy = GetCurrentUserId()
                };

                dbContext.Routes.Add(route);
            }

            routeId = route.Id;
        }

        var trip = new Trip
        {
            BusId = request.BusId,
            RouteId = routeId,
            Status = request.Status,
            DepartureTime = departureUtc,
            ArrivalTime = arrivalUtc,
            PricePerSeat = request.PricePerSeat
        };

        dbContext.Trips.Add(trip);
        await dbContext.SaveChangesAsync();

        // ✅ RETURN SAFE DTO
        return CreatedAtAction(nameof(GetTripById), new { id = trip.Id }, new
        {
            trip.Id,
            trip.BusId,
            trip.RouteId,
            BusVehicleNumber = bus.VehicleNumber,
            Route = new
            {
                Id = routeId
            },
            Status = trip.Status.ToString(),
            trip.DepartureTime,
            trip.ArrivalTime,
            trip.PricePerSeat,
            trip.CreatedAt
        });
    }

    // 🔥 SEARCH TRIPS
    [HttpGet]
    public async Task<IActionResult> SearchTrips(
        [FromQuery] Guid? fromId,
        [FromQuery] Guid? toId,
        [FromQuery] DateTime? date)
    {
        var query = dbContext.Trips
            .Include(x => x.Route)
                .ThenInclude(x => x.From)
            .Include(x => x.Route)
                .ThenInclude(x => x.To)
            .Include(x => x.Bus)
            .AsQueryable();

        if (fromId.HasValue)
            query = query.Where(x => x.Route.FromId == fromId.Value);

        if (toId.HasValue)
            query = query.Where(x => x.Route.ToId == toId.Value);

        if (date.HasValue)
        {
            var day = date.Value.Date;
            var next = day.AddDays(1);
            query = query.Where(x => x.DepartureTime >= day && x.DepartureTime < next);
        }

        var trips = await query
            .OrderBy(x => x.DepartureTime)
            .Select(x => new
            {
                x.Id,
                x.BusId,
                x.RouteId,
                BusVehicleNumber = x.Bus.VehicleNumber,
                RouteLabel = x.Route.From.City + ", " + x.Route.From.State + " -> " + x.Route.To.City + ", " + x.Route.To.State,
                Status = x.Status.ToString(),
                x.DepartureTime,
                x.ArrivalTime,
                x.PricePerSeat,
                x.CreatedAt
            })
            .ToListAsync();

        return Ok(trips);
    }

    // 🔥 GET BY ID
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTripById(Guid id)
    {
        var trip = await dbContext.Trips
            .Include(x => x.Bus)
            .Include(x => x.Route)
                .ThenInclude(x => x.From)
            .Include(x => x.Route)
                .ThenInclude(x => x.To)
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.BusId,
                x.RouteId,
                BusVehicleNumber = x.Bus.VehicleNumber,
                RouteLabel = x.Route.From.City + ", " + x.Route.From.State + " -> " + x.Route.To.City + ", " + x.Route.To.State,
                Status = x.Status.ToString(),
                x.DepartureTime,
                x.ArrivalTime,
                x.PricePerSeat,
                x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (trip is null)
            return NotFound("Trip not found.");

        return Ok(trip);
    }

    // 🔥 UPDATE
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Operator) + "," + nameof(UserRole.Admin))]
    public async Task<IActionResult> UpdateTrip(Guid id, [FromBody] UpdateTripRequest request)
    {
        var trip = await dbContext.Trips
            .Include(x => x.Bus)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (trip is null)
            return NotFound("Trip not found.");

        if (!User.IsInRole(nameof(UserRole.Admin)) && trip.Bus.OperatorId != GetCurrentUserId())
            return Forbid();

        if (trip.Bus.Status != BusStatus.Approved)
            return BadRequest("Trip can be updated only for approved bus.");

        if (!TryNormalizeAndValidateTripTimes(request.DepartureTime, request.ArrivalTime, out var departureUtc, out var arrivalUtc, out var validationError))
            return BadRequest(validationError);

        trip.Status = request.Status;
        trip.DepartureTime = departureUtc;
        trip.ArrivalTime = arrivalUtc;
        trip.PricePerSeat = request.PricePerSeat;

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            trip.Id,
            Status = trip.Status.ToString(),
            trip.DepartureTime,
            trip.ArrivalTime,
            trip.PricePerSeat
        });
    }

    // 🔥 CANCEL
    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = nameof(UserRole.Operator) + "," + nameof(UserRole.Admin))]
    public async Task<IActionResult> CancelTrip(Guid id)
    {
        var trip = await dbContext.Trips
            .Include(x => x.Bus)
            .Include(x => x.Route)
                .ThenInclude(x => x.From)
            .Include(x => x.Route)
                .ThenInclude(x => x.To)
            .Include(x => x.Tickets)
                .ThenInclude(x => x.User)
            .Include(x => x.Tickets)
                .ThenInclude(x => x.SeatBookings)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (trip is null)
            return NotFound("Trip not found.");

        if (!User.IsInRole(nameof(UserRole.Admin)) && trip.Bus.OperatorId != GetCurrentUserId())
            return Forbid();

        if (trip.Status == TripStatus.Cancelled)
            return Conflict("Trip is already cancelled.");

        trip.Status = TripStatus.Cancelled;

        foreach (var ticket in trip.Tickets.Where(x => x.Status != TicketStatus.Cancelled))
        {
            ticket.Status = TicketStatus.Cancelled;

            foreach (var seat in ticket.SeatBookings)
            {
                seat.Status = SeatBookingStatus.Cancelled;
                seat.ReservedUntil = null;
            }
        }

        await dbContext.SaveChangesAsync();

        var passengerEmails = trip.Tickets
            .Select(x => x.User.Email)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var emailsSent = await TrySendTripCancellationEmailAsync(trip, passengerEmails);

        return Ok(new
        {
            trip.Id,
            Status = trip.Status.ToString(),
            NotifiedPassengers = passengerEmails.Count,
            EmailsSent = emailsSent
        });
    }

    // 🔥 SEATS
    [HttpGet("{id:guid}/seats")]
    public async Task<IActionResult> GetTripSeats(Guid id)
    {
        var trip = await dbContext.Trips
            .Include(x => x.Bus)
                .ThenInclude(x => x.Layout)
            .Include(x => x.Route)
                .ThenInclude(x => x.From)
            .Include(x => x.Route)
                .ThenInclude(x => x.To)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (trip is null)
            return NotFound("Trip not found.");

        if (trip.Bus is null || trip.Bus.Layout is null)
            return BadRequest("Bus layout is not configured for this trip.");

        var routeLabel = trip.Route?.From is not null && trip.Route?.To is not null
            ? trip.Route.From.City + ", " + trip.Route.From.State + " -> " + trip.Route.To.City + ", " + trip.Route.To.State
            : "Route details unavailable";

        var seats = await dbContext.SeatBookings
            .Where(x => x.TripId == id)
            .Select(x => new
            {
                x.SeatNumber,
                Status = x.Status.ToString(),
                x.ReservedUntil
            })
            .ToListAsync();

        return Ok(new
        {
            TripId = trip.Id,
            trip.BusId,
            RouteLabel = routeLabel,
            Layout = trip.Bus.Layout.Config.RootElement,
            Seats = seats
        });
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static bool TryNormalizeAndValidateTripTimes(
        DateTime departureTime,
        DateTime arrivalTime,
        out DateTime departureUtc,
        out DateTime arrivalUtc,
        out string? validationError)
    {
        departureUtc = NormalizeToUtc(departureTime);
        arrivalUtc = NormalizeToUtc(arrivalTime);

        if (departureUtc <= DateTime.UtcNow)
        {
            validationError = "Departure time cannot be in the past.";
            return false;
        }

        if (arrivalUtc <= departureUtc)
        {
            validationError = "Arrival time must be later than departure time.";
            return false;
        }

        if (arrivalUtc - departureUtc < TimeSpan.FromMinutes(15))
        {
            validationError = "Trip duration must be at least 15 minutes.";
            return false;
        }

        validationError = null;
        return true;
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private async Task<bool> TrySendTripCancellationEmailAsync(Trip trip, IReadOnlyCollection<string> recipients)
    {
        if (recipients.Count == 0)
        {
            return true;
        }

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

        var route = trip.Route?.From is not null && trip.Route?.To is not null
            ? $"{trip.Route.From.City}, {trip.Route.From.State} -> {trip.Route.To.City}, {trip.Route.To.State}"
            : "Route details unavailable";

        var body = new StringBuilder()
            .AppendLine("Your trip has been cancelled by the operator.")
            .AppendLine($"Trip ID: {trip.Id}")
            .AppendLine($"Bus ID: {trip.BusId}")
            .AppendLine($"Route: {route}")
            .AppendLine($"Departure (UTC): {trip.DepartureTime:yyyy-MM-dd HH:mm}")
            .AppendLine("We apologize for the inconvenience.")
            .ToString();

        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl
            };

            if (!string.IsNullOrWhiteSpace(username))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            foreach (var recipient in recipients)
            {
                using var message = new MailMessage(fromEmail, recipient)
                {
                    Subject = $"Trip Cancelled - {trip.Id}",
                    Body = body
                };

                await client.SendMailAsync(message);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send trip cancellation emails for trip {TripId}", trip.Id);
            return false;
        }
    }
}