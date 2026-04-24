using Bus_Booking_System.Data;
using Bus_Booking_System.Models.DTOs;
using Bus_Booking_System.Models.Entities;
using Bus_Booking_System.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RouteEntity = Bus_Booking_System.Models.Entities.Route;

namespace Bus_Booking_System.Controllers;

[ApiController]
[Route("trips")]
[Authorize]
public class TripsController(AppDbContext dbContext) : ControllerBase
{
    // 🔥 CREATE TRIP
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Operator) + "," + nameof(UserRole.Admin))]
    public async Task<IActionResult> CreateTrip([FromBody] CreateTripRequest request)
    {
        var bus = await dbContext.Buses.FirstOrDefaultAsync(x => x.Id == request.BusId);
        if (bus is null)
            return BadRequest("Invalid bus id.");

        if (!User.IsInRole(nameof(UserRole.Admin)) && bus.OperatorId != GetCurrentUserId())
            return Forbid();

        Guid routeId;

        if (request.RouteId.HasValue)
        {
            var existingRoute = await dbContext.Routes.FirstOrDefaultAsync(x => x.Id == request.RouteId.Value);
            if (existingRoute is null)
                return BadRequest("Invalid route id.");

            routeId = existingRoute.Id;
        }
        else
        {
            if (!request.FromId.HasValue || !request.ToId.HasValue)
                return BadRequest("Either routeId or fromId/toId is required.");

            if (request.FromId.Value == request.ToId.Value)
                return BadRequest("fromId and toId cannot be same.");

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
            DepartureTime = request.DepartureTime,
            ArrivalTime = request.ArrivalTime,
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

        trip.Status = request.Status;
        trip.DepartureTime = request.DepartureTime;
        trip.ArrivalTime = request.ArrivalTime;
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
            .FirstOrDefaultAsync(x => x.Id == id);

        if (trip is null)
            return NotFound("Trip not found.");

        if (!User.IsInRole(nameof(UserRole.Admin)) && trip.Bus.OperatorId != GetCurrentUserId())
            return Forbid();

        trip.Status = TripStatus.Cancelled;
        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            trip.Id,
            Status = trip.Status.ToString()
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
}