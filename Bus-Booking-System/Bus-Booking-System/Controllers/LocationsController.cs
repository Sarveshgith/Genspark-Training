using Bus_Booking_System.Data;
using Bus_Booking_System.Models.DTOs;
using Bus_Booking_System.Models.Entities;
using Bus_Booking_System.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bus_Booking_System.Controllers;

[ApiController]
[Route("locations")]
[Authorize]
public class LocationsController(AppDbContext dbContext) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> CreateLocation([FromBody] CreateLocationRequest request)
    {
        var city = request.City.Trim();
        var state = request.State.Trim();

        var exists = await dbContext.Locations.AnyAsync(x => x.City == city && x.State == state);
        if (exists)
        {
            return Conflict("Location with the same city and state already exists.");
        }

        var location = new Location
        {
            City = city,
            State = state,
            Status = request.Status
        };

        dbContext.Locations.Add(location);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLocationById), new { id = location.Id }, location);
    }

    [HttpGet]
    public async Task<IActionResult> GetLocations()
    {
        var locations = await dbContext.Locations
            .OrderBy(x => x.City)
            .ThenBy(x => x.State)
            .ToListAsync();

        return Ok(locations);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetLocationById(Guid id)
    {
        var location = await dbContext.Locations.FirstOrDefaultAsync(x => x.Id == id);
        if (location is null)
        {
            return NotFound("Location not found.");
        }

        return Ok(location);
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> UpdateLocation(Guid id, [FromBody] UpdateLocationRequest request)
    {
        var location = await dbContext.Locations.FirstOrDefaultAsync(x => x.Id == id);
        if (location is null)
        {
            return NotFound("Location not found.");
        }

        var city = request.City.Trim();
        var state = request.State.Trim();

        var duplicate = await dbContext.Locations.AnyAsync(x => x.Id != id && x.City == city && x.State == state);
        if (duplicate)
        {
            return Conflict("Location with the same city and state already exists.");
        }

        location.City = city;
        location.State = state;
        location.Status = request.Status;

        await dbContext.SaveChangesAsync();
        return Ok(location);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> DeleteLocation(Guid id)
    {
        var location = await dbContext.Locations.FirstOrDefaultAsync(x => x.Id == id);
        if (location is null)
        {
            return NotFound("Location not found.");
        }

        dbContext.Locations.Remove(location);

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict("Location is referenced by other records.");
        }

        return NoContent();
    }
}
