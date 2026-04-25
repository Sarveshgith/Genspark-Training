using Bus_Booking_System.Data;
using Bus_Booking_System.Models.DTOs;
using Bus_Booking_System.Models.Entities;
using Bus_Booking_System.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bus_Booking_System.Controllers;

[ApiController]
[Route("buses")]
[Authorize]
public class BusesController(AppDbContext dbContext) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Operator) + "," + nameof(UserRole.Admin))]
    public async Task<IActionResult> CreateBus([FromBody] CreateBusRequest request)
    {
        var operatorRow = await dbContext.Operators.FirstOrDefaultAsync(x => x.UserId == request.OperatorId);
        if (operatorRow is null)
        {
            return BadRequest("Invalid operator id.");
        }

        if (!User.IsInRole(nameof(UserRole.Admin)) && operatorRow.Status != OperatorStatus.Approved)
        {
            return BadRequest("Operator account is not approved. Bus creation is allowed only for approved operators.");
        }

        if (!await dbContext.BusLayouts.AnyAsync(x => x.Id == request.LayoutId))
        {
            return BadRequest("Invalid layout id.");
        }

        if (!User.IsInRole(nameof(UserRole.Admin)) && GetCurrentUserId() != request.OperatorId)
        {
            return Forbid();
        }

        var exists = await dbContext.Buses.AnyAsync(x => x.VehicleNumber == request.VehicleNumber.Trim());
        if (exists)
        {
            return Conflict("Vehicle number already exists.");
        }

        var bus = new Bus
        {
            OperatorId = request.OperatorId,
            LayoutId = request.LayoutId,
            VehicleNumber = request.VehicleNumber.Trim(),
            Status = request.Status
        };

        dbContext.Buses.Add(bus);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBusById), new { id = bus.Id }, bus);
    }

    [HttpGet]
    public async Task<IActionResult> GetBuses()
    {
        var query = dbContext.Buses
            .Include(x => x.Operator)
            .Include(x => x.Layout)
            .AsQueryable();

        if (User.IsInRole(nameof(UserRole.Operator)))
        {
            var me = GetCurrentUserId();
            query = query.Where(x => x.OperatorId == me);
        }

        var buses = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.OperatorId,
                x.LayoutId,
                x.VehicleNumber,
                Status = x.Status.ToString(),
                x.ApprovedBy,
                x.ApprovedAt,
                x.CreatedAt,
                Layout = new
                {
                    x.Layout.TotalSeats
                }
            })
            .ToListAsync();

        return Ok(buses);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBusById(Guid id)
    {
        var bus = await dbContext.Buses
            .Include(x => x.Layout)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (bus is null)
        {
            return NotFound("Bus not found.");
        }

        if (!User.IsInRole(nameof(UserRole.Admin)) && bus.OperatorId != GetCurrentUserId())
        {
            return Forbid();
        }

        return Ok(new
        {
            bus.Id,
            bus.OperatorId,
            bus.LayoutId,
            bus.VehicleNumber,
            Status = bus.Status.ToString(),
            bus.ApprovedBy,
            bus.ApprovedAt,
            bus.CreatedAt,
            Layout = new
            {
                bus.Layout.TotalSeats,
                Config = bus.Layout.Config.RootElement
            }
        });
    }

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Operator) + "," + nameof(UserRole.Admin))]
    public async Task<IActionResult> UpdateBus(Guid id, [FromBody] UpdateBusRequest request)
    {
        var bus = await dbContext.Buses.FirstOrDefaultAsync(x => x.Id == id);
        if (bus is null)
        {
            return NotFound("Bus not found.");
        }

        if (!User.IsInRole(nameof(UserRole.Admin)) && bus.OperatorId != GetCurrentUserId())
        {
            return Forbid();
        }

        var duplicateVehicle = await dbContext.Buses.AnyAsync(x => x.Id != id && x.VehicleNumber == request.VehicleNumber.Trim());
        if (duplicateVehicle)
        {
            return Conflict("Vehicle number already exists.");
        }

        if (!await dbContext.BusLayouts.AnyAsync(x => x.Id == request.LayoutId))
        {
            return BadRequest("Invalid layout id.");
        }

        bus.LayoutId = request.LayoutId;
        bus.VehicleNumber = request.VehicleNumber.Trim();
        bus.Status = request.Status;

        await dbContext.SaveChangesAsync();
        return Ok(bus);
    }

    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> ApproveBus(Guid id, [FromBody] ApproveBusRequest request)
    {
        var bus = await dbContext.Buses.FirstOrDefaultAsync(x => x.Id == id);
        if (bus is null)
        {
            return NotFound("Bus not found.");
        }

        bus.Status = request.Status;
        bus.ApprovedBy = GetCurrentUserId();
        bus.ApprovedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            bus.Id,
            Status = bus.Status.ToString(),
            bus.ApprovedBy,
            bus.ApprovedAt
        });
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
