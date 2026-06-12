using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Services.Interfaces;

namespace OrderNKitchenMS_API.Controllers;

[ApiController]
[Route("api/bills")]
[Authorize]
public class BillController : ControllerBase
{
    private readonly IBillService _billService;
    public BillController(IBillService billService)
    {
        _billService = billService;
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<ActionResult<BillDto>> CreateBill([FromBody] BillCreateDto billCreateDto)
    {
        var billDto = await _billService.CreateBillAsync(billCreateDto);
        return Ok(billDto);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BillDto>>> GetAllBills()
    {
        var bills = await _billService.GetAllBillsAsync();
        return Ok(bills);
    }

    [Authorize(Policy = "CanPlaceOrder")]
    [HttpGet("order/{orderId:int}")]
    public async Task<ActionResult<BillDto>> GetBillByOrderId(int orderId)
    {
        var bill = await _billService.GetBillByOrderIdAsync(orderId);
        return Ok(bill);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<BillDto>> UpdateBill(int id, [FromBody] BillCreateDto billCreateDto)
    {
        var updatedBill = await _billService.UpdateBillAsync(id, billCreateDto);
        return Ok(updatedBill);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateBillStatus(int id, [FromBody] string status)
    {
        await _billService.UpdateBillStatusAsync(id, status);
        return NoContent();
    }

    [Authorize(Policy = "CanPlaceOrder")]
    [HttpGet("order/{orderid:int}/pdf")]
    public async Task<IActionResult> GenerateBillPdf(int orderid)
    {
        var pdfBytes = await _billService.GenerateBillPdfAsync(orderid);
        return File(pdfBytes, "application/pdf", $"Bill_Order_{orderid}.pdf");
    }
}
