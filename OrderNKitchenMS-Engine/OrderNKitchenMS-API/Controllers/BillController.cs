using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Services.Interfaces;
using Microsoft.Extensions.Logging;
using OrderNKitchenMS_API.Utils;

namespace OrderNKitchenMS_API.Controllers;

[ApiController]
[Route("api/bills")]
[Authorize]
public class BillController : ControllerBase
{
    private readonly IBillService _billService;
    private readonly ILogger<BillController> _logger;

    public BillController(IBillService billService, ILogger<BillController> logger)
    {
        _billService = billService;
        _logger = logger;
    }

    [Authorize(Policy = "AdminOrWaiter")]
    [HttpPost]
    public async Task<ActionResult<BillDto>> CreateBill([FromBody] BillCreateDto billCreateDto)
    {
        Validation.RequireNotNull(billCreateDto, nameof(billCreateDto));
        _logger.LogInformation("CreateBill requested for Order ID: {OrderId}", billCreateDto.OrderId);
        var billDto = await _billService.CreateBillAsync(billCreateDto);
        return CreatedAtAction(nameof(GetBillByOrderId), new { orderId = billDto.OrderId }, billDto);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BillDto>>> GetAllBills()
    {
        _logger.LogInformation("GetAllBills requested.");
        var bills = await _billService.GetAllBillsAsync();
        return Ok(bills);
    }

    [Authorize(Policy = "All")]
    [HttpGet("order/{orderId:int}")]
    public async Task<ActionResult<BillDto>> GetBillByOrderId(int orderId)
    {
        Validation.ValidateId(orderId, nameof(orderId));
        _logger.LogInformation("GetBillByOrderId requested for Order ID: {OrderId}", orderId);
        var bill = await _billService.GetBillByOrderIdAsync(orderId);
        return Ok(bill);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<BillDto>> UpdateBill(int id, [FromBody] BillCreateDto billCreateDto)
    {
        Validation.ValidateRequest(id, billCreateDto);
        _logger.LogInformation("UpdateBill requested for Bill ID: {Id}", id);
        var updatedBill = await _billService.UpdateBillAsync(id, billCreateDto);
        return Ok(updatedBill);
    }

    [Authorize(Policy = "All")]
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateBillStatus(int id, [FromBody] string status)
    {
        Validation.ValidateId(id);
        Validation.RequireNonEmptyString(status, nameof(status), "Status cannot be empty.");
        _logger.LogInformation("UpdateBillStatus requested for Bill ID: {Id}, Status: '{Status}'", id, status);
        await _billService.UpdateBillStatusAsync(id, status);
        return NoContent();
    }

    [Authorize(Policy = "All")]
    [HttpPatch("splits/{splitId:int}/pay")]
    public async Task<IActionResult> PayBillSplit(int splitId)
    {
        Validation.ValidateId(splitId);
        _logger.LogInformation("PayBillSplit requested for Split ID: {SplitId}", splitId);
        await _billService.PayBillSplitAsync(splitId);
        return NoContent();
    }

    [Authorize(Policy = "All")]
    [HttpGet("order/{orderid:int}/pdf")]
    public async Task<IActionResult> GenerateBillPdf(int orderid)
    {
        Validation.ValidateId(orderid, nameof(orderid));
        _logger.LogInformation("GenerateBillPdf requested for Order ID: {OrderId}", orderid);
        var pdfBytes = await _billService.GenerateBillPdfAsync(orderid);
        return File(pdfBytes, "application/pdf", $"Bill_Order_{orderid}.pdf");
    }
}
