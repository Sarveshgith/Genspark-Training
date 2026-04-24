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
[Route("operators")]
[Authorize]
public class OperatorsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> GetOperators()
    {
        var operators = await dbContext.Operators
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
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

        return Ok(operators);
    }

    [HttpGet("{operatorId:guid}")]
    public async Task<IActionResult> GetOperatorById(Guid operatorId)
    {
        if (!IsAdminOrSelfOperator(operatorId))
        {
            return Forbid();
        }

        var op = await dbContext.Operators
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.UserId == operatorId);

        if (op is null)
        {
            return NotFound("Operator not found.");
        }

        return Ok(new
        {
            op.UserId,
            op.LicenseNumber,
            Status = op.Status.ToString(),
            op.ApprovedBy,
            op.CreatedAt,
            User = new
            {
                op.User.Name,
                op.User.Email,
                op.User.Phone
            }
        });
    }

    [HttpGet("{operatorId:guid}/locations")]
    public async Task<IActionResult> GetOperatorLocations(Guid operatorId)
    {
        if (!IsAdminOrSelfOperator(operatorId))
        {
            return Forbid();
        }

        var exists = await dbContext.Operators.AnyAsync(x => x.UserId == operatorId);
        if (!exists)
        {
            return NotFound("Operator not found.");
        }

        var locations = await dbContext.OperatorLocations
            .Where(x => x.OperatorId == operatorId)
            .Include(x => x.Location)
            .Select(x => new
            {
                x.Id,
                x.OperatorId,
                x.LocationId,
                x.Address,
                x.IsActive,
                Location = new
                {
                    x.Location.City,
                    x.Location.State,
                    Status = x.Location.Status.ToString()
                }
            })
            .ToListAsync();

        return Ok(locations);
    }

    [HttpPatch("{operatorId:guid}/status")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> UpdateOperatorStatus(Guid operatorId, [FromBody] UpdateOperatorStatusRequest request)
    {
        var op = await dbContext.Operators.FirstOrDefaultAsync(x => x.UserId == operatorId);
        if (op is null)
        {
            return NotFound("Operator not found.");
        }

        op.Status = request.Status;
        op.ApprovedBy = GetCurrentUserId();

        await dbContext.SaveChangesAsync();
        return Ok(new { op.UserId, Status = op.Status.ToString(), op.ApprovedBy });
    }

    [HttpPatch("{operatorId:guid}/locations/status/{id:guid}")]
    public async Task<IActionResult> UpdateOperatorLocationStatus(Guid operatorId, Guid id, [FromBody] UpdateOperatorLocationStatusRequest request)
    {
        if (!IsAdminOrSelfOperator(operatorId))
        {
            return Forbid();
        }

        var location = await dbContext.OperatorLocations
            .FirstOrDefaultAsync(x => x.Id == id && x.OperatorId == operatorId);

        if (location is null)
        {
            return NotFound("Operator location mapping not found.");
        }

        location.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync();

        return Ok(new { location.Id, location.OperatorId, location.IsActive });
    }

    [HttpPost("{operatorId:guid}/locations")]
    public async Task<IActionResult> AddOperatorLocation(Guid operatorId, [FromBody] AddOperatorLocationRequest request)
    {
        if (!IsAdminOrSelfOperator(operatorId))
        {
            return Forbid();
        }

        var operatorExists = await dbContext.Operators.AnyAsync(x => x.UserId == operatorId);
        if (!operatorExists)
        {
            return NotFound("Operator not found.");
        }

        var location = await dbContext.Locations.FirstOrDefaultAsync(x => x.Id == request.LocationId);
        if (location is null)
        {
            return BadRequest("Invalid location id.");
        }

        if (location.Status != LocationStatus.Approved)
        {
            return BadRequest("Location is not approved yet. Add office address only after admin approval.");
        }

        var address = request.Address.Trim();
        if (string.IsNullOrWhiteSpace(address))
        {
            return BadRequest("Address is required.");
        }

        var row = new OperatorLocation
        {
            OperatorId = operatorId,
            LocationId = request.LocationId,
            Address = address,
            IsActive = true
        };

        dbContext.OperatorLocations.Add(row);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOperatorLocations), new { operatorId }, new
        {
            row.Id,
            row.OperatorId,
            row.LocationId,
            row.Address,
            row.IsActive
        });
    }

    [HttpPost("{operatorId:guid}/location-requests")]
    public async Task<IActionResult> RequestLocationApproval(Guid operatorId, [FromBody] RequestLocationApprovalRequest request)
    {
        if (!IsAdminOrSelfOperator(operatorId))
        {
            return Forbid();
        }

        var operatorExists = await dbContext.Operators.AnyAsync(x => x.UserId == operatorId);
        if (!operatorExists)
        {
            return NotFound("Operator not found.");
        }

        var city = request.City.Trim();
        var state = request.State.Trim();
        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(state))
        {
            return BadRequest("City and state are required.");
        }

        var existing = await dbContext.Locations
            .FirstOrDefaultAsync(x => x.City == city && x.State == state);

        if (existing is not null)
        {
            if (existing.Status == LocationStatus.Approved)
            {
                return Conflict("Location already approved. You can now add office address for this location.");
            }

            if (existing.Status == LocationStatus.Pending)
            {
                return Conflict("Location request is already pending admin approval.");
            }

            existing.Status = LocationStatus.Pending;
            await dbContext.SaveChangesAsync();
            return Ok(new
            {
                existing.Id,
                existing.City,
                existing.State,
                Status = existing.Status.ToString(),
                Message = "Existing rejected location has been resubmitted for approval."
            });
        }

        var location = new Location
        {
            City = city,
            State = state,
            Status = LocationStatus.Pending
        };

        dbContext.Locations.Add(location);
        await dbContext.SaveChangesAsync();

        return Ok(new
        {
            location.Id,
            location.City,
            location.State,
            Status = location.Status.ToString(),
            Message = "Location request submitted for admin approval."
        });
    }

    private bool IsAdminOrSelfOperator(Guid operatorId)
    {
        return User.IsInRole(nameof(UserRole.Admin)) ||
               (User.IsInRole(nameof(UserRole.Operator)) && GetCurrentUserId() == operatorId);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
