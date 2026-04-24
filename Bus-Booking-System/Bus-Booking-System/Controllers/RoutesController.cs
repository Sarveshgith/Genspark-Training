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
[Route("routes")]
[Authorize]
public class RoutesController(AppDbContext dbContext) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> CreateRoute([FromBody] CreateRouteRequest request)
    {
        if (request.FromId == request.ToId)
        {
            return BadRequest("from_id and to_id cannot be same.");
        }

        var fromExists = await dbContext.Locations.AnyAsync(x => x.Id == request.FromId);
        var toExists = await dbContext.Locations.AnyAsync(x => x.Id == request.ToId);
        if (!fromExists || !toExists)
        {
            return BadRequest("Invalid from_id or to_id.");
        }

        var exists = await dbContext.Routes.AnyAsync(x => x.FromId == request.FromId && x.ToId == request.ToId);
        if (exists)
        {
            return Conflict("Route already exists for the same from/to.");
        }

        var row = new RouteEntity
        {
            FromId = request.FromId,
            ToId = request.ToId,
            Status = request.Status,
            CreatedBy = GetCurrentUserId()
        };

        dbContext.Routes.Add(row);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRouteById), new { id = row.Id }, row);
    }

    [HttpGet]
    public async Task<IActionResult> GetRoutes()
    {
        var routes = await dbContext.Routes
            .Include(x => x.From)
            .Include(x => x.To)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.FromId,
                x.ToId,
                Status = x.Status.ToString(),
                x.CreatedBy,
                x.CreatedAt,
                From = new { x.From.City, x.From.State },
                To = new { x.To.City, x.To.State }
            })
            .ToListAsync();

        return Ok(routes);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetRouteById(Guid id)
    {
        var route = await dbContext.Routes
            .Include(x => x.From)
            .Include(x => x.To)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (route is null)
        {
            return NotFound("Route not found.");
        }

        return Ok(new
        {
            route.Id,
            route.FromId,
            route.ToId,
            Status = route.Status.ToString(),
            route.CreatedBy,
            route.CreatedAt,
            From = new { route.From.City, route.From.State },
            To = new { route.To.City, route.To.State }
        });
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> UpdateRoute(Guid id, [FromBody] UpdateRouteRequest request)
    {
        var route = await dbContext.Routes.FirstOrDefaultAsync(x => x.Id == id);
        if (route is null)
        {
            return NotFound("Route not found.");
        }

        route.Status = request.Status;
        await dbContext.SaveChangesAsync();

        return Ok(route);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
