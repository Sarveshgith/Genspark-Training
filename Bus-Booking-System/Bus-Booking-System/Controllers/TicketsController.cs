using Bus_Booking_System.Data;
using Bus_Booking_System.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;

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

    [HttpGet("{ticketId:guid}/download-pdf")]
    public async Task<IActionResult> DownloadPdf(Guid ticketId)
    {
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

        if (!await HasAccess(ticket.UserId, ticket.Id))
        {
            return Forbid();
        }

        // Only confirmed tickets can be downloaded as PDF
        if (ticket.Status != TicketStatus.Confirmed)
        {
            return BadRequest("Only confirmed tickets can be downloaded. Cancelled tickets cannot be downloaded.");
        }

        try
        {
            using var pdfStream = new MemoryStream();
            using (var writer = new PdfWriter(pdfStream))
            using (var pdf = new PdfDocument(writer))
            using (var document = new Document(pdf))
            {
                writer.SetCloseStream(false);

                // Title
                document.Add(new Paragraph("BUS BOOKING TICKET")
                    .SetFontSize(20)
                    .SetBold()
                    .SetMarginBottom(20));

                // Ticket Number
                document.Add(new Paragraph($"Booking Reference: {ticket.BookingRef}")
                    .SetFontSize(12)
                    .SetBold()
                    .SetMarginBottom(10));

                // Passenger Details
                document.Add(new Paragraph("PASSENGER DETAILS")
                    .SetFontSize(12)
                    .SetBold()
                    .SetMarginTop(15)
                    .SetMarginBottom(5));

                document.Add(new Paragraph($"Name: {ticket.User.Name}"));
                document.Add(new Paragraph($"Email: {ticket.User.Email}"));
                document.Add(new Paragraph($"Phone: {ticket.User.Phone}").SetMarginBottom(15));

                // Journey Details
                document.Add(new Paragraph("JOURNEY DETAILS")
                    .SetFontSize(12)
                    .SetBold()
                    .SetMarginBottom(5));

                document.Add(new Paragraph($"Bus ID: {ticket.Trip.BusId}"));
                document.Add(new Paragraph($"Route: {ticket.Trip.Route.From.City}, {ticket.Trip.Route.From.State} → {ticket.Trip.Route.To.City}, {ticket.Trip.Route.To.State}"));
                document.Add(new Paragraph($"Departure: {ticket.Trip.DepartureTime:yyyy-MM-dd HH:mm:ss} UTC"));
                document.Add(new Paragraph($"Arrival: {ticket.Trip.ArrivalTime:yyyy-MM-dd HH:mm:ss} UTC").SetMarginBottom(15));

                // Seat Details
                document.Add(new Paragraph("SELECTED SEATS")
                    .SetFontSize(12)
                    .SetBold()
                    .SetMarginBottom(5));

                var seats = string.Join(", ", ticket.SeatBookings.Select(x => x.SeatNumber));
                document.Add(new Paragraph($"Seats: {seats}").SetMarginBottom(15));

                // Fare Breakdown
                document.Add(new Paragraph("FARE BREAKDOWN")
                    .SetFontSize(12)
                    .SetBold()
                    .SetMarginBottom(5));

                var convenience = Math.Round(ticket.BaseAmount * 0.05m, 2);
                var service = 25m;
                var taxable = ticket.BaseAmount + convenience + service;
                var tax = Math.Round(taxable * 0.18m, 2);

                document.Add(new Paragraph($"Base Fare: ₹ {ticket.BaseAmount:0.00}"));
                document.Add(new Paragraph($"Convenience Fee: ₹ {convenience:0.00}"));
                document.Add(new Paragraph($"Service Fee: ₹ {service:0.00}"));
                document.Add(new Paragraph($"Tax (18%): ₹ {tax:0.00}"));
                document.Add(new Paragraph($"Total Amount: ₹ {ticket.TotalAmount:0.00}")
                    .SetBold()
                    .SetMarginBottom(15));

                // Payment Details
                document.Add(new Paragraph("PAYMENT DETAILS")
                    .SetFontSize(12)
                    .SetBold()
                    .SetMarginBottom(5));

                document.Add(new Paragraph($"Payment Status: {ticket.PaymentStatus}"));
                document.Add(new Paragraph($"Payment Reference: {ticket.PaymentRef}").SetMarginBottom(15));

                // Footer
                document.Add(new Paragraph("Thank you for booking with us!")
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .SetFontSize(10)
                    .SetMarginTop(20));

                document.Add(new Paragraph($"Booked on: {ticket.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC")
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .SetFontSize(8));
            }

            var pdfBytes = pdfStream.ToArray();
            return File(pdfBytes, "application/pdf", $"Ticket_{ticket.BookingRef}.pdf");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error generating PDF: {ex.Message}");
        }
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
