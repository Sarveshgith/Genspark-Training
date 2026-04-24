using Bus_Booking_System.Data;
using Bus_Booking_System.Models.DTOs;
using Bus_Booking_System.Models.Entities;
using Bus_Booking_System.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Bus_Booking_System.Controllers;

[ApiController]
[Route("bus-layouts")]
[Authorize]
public class BusLayoutsController(AppDbContext dbContext) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Operator) + "," + nameof(UserRole.Admin))]
    public async Task<IActionResult> CreateLayout([FromBody] CreateBusLayoutRequest request)
    {
        var row = new BusLayout
        {
            TotalSeats = request.TotalSeats,
            Config = JsonDocument.Parse(request.Config.GetRawText())
        };

        dbContext.BusLayouts.Add(row);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetLayoutById), new { id = row.Id }, new
        {
            row.Id,
            row.TotalSeats,
            Config = JsonDocument.Parse(row.Config.RootElement.GetRawText()),
            row.CreatedAt
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetLayouts()
    {
        var rows = await dbContext.BusLayouts
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return Ok(rows.Select(x => new
        {
            x.Id,
            x.TotalSeats,
            Config = x.Config.RootElement,
            x.CreatedAt
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetLayoutById(Guid id)
    {
        var row = await dbContext.BusLayouts.FirstOrDefaultAsync(x => x.Id == id);
        if (row is null)
        {
            return NotFound("Bus layout not found.");
        }

        return Ok(new
        {
            row.Id,
            row.TotalSeats,
            Config = row.Config.RootElement,
            row.CreatedAt
        });
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Operator) + "," + nameof(UserRole.Admin))]
    public async Task<IActionResult> UpdateLayout(Guid id, [FromBody] UpdateBusLayoutRequest request)
    {
        var row = await dbContext.BusLayouts.FirstOrDefaultAsync(x => x.Id == id);
        if (row is null)
        {
            return NotFound("Bus layout not found.");
        }

        row.TotalSeats = request.TotalSeats;
        row.Config = JsonDocument.Parse(request.Config.GetRawText());

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            row.Id,
            row.TotalSeats,
            Config = row.Config.RootElement,
            row.CreatedAt
        });
    }
}
