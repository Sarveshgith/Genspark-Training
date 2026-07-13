using System;
using Microsoft.AspNetCore.Http.HttpResults;
using OrderNKitchenMS_API.Exceptions;
using OrderNKitchenMS_API.Models.DTOs;
using OrderNKitchenMS_API.Models.Entities;
using OrderNKitchenMS_API.Models.Enums;
using OrderNKitchenMS_API.Repositories.Interfaces;
using OrderNKitchenMS_API.Services.Interfaces;
using OrderNKitchenMS_API.Utils;
using Microsoft.Extensions.Logging;

namespace OrderNKitchenMS_API.Services;

public class BillService : IBillService
{
    private readonly IBillRepository _billRepository;
    private readonly IOrderService _orderService;
    private readonly ISignalService _signalService;
    private readonly ILogger<BillService> _logger;

    public BillService(IBillRepository billRepository, IOrderService orderService, ISignalService signalService, ILogger<BillService> logger)
    {
        _billRepository = billRepository;
        _orderService = orderService;
        _signalService = signalService;
        _logger = logger;
    }

    // Generates a new bill for a ready order.
    public async Task<BillDto> CreateBillAsync(BillCreateDto billCreateDto)
    {
        _logger.LogInformation("CreateBillAsync started for Order ID: {OrderId}", billCreateDto.OrderId);
        Validation.RequireNotNull(billCreateDto, nameof(billCreateDto), "Bill data is required.");
        var order = await _orderService.GetOrderByIdAsync(billCreateDto.OrderId);
        if(order == null)
        {
            _logger.LogWarning("CreateBillAsync failed: Order with ID {OrderId} not found.", billCreateDto.OrderId);
            throw new NotFoundException($"Order with ID {billCreateDto.OrderId} not found.");
        }

        var existingBill = await _billRepository.GetBillByOrderIdAsync(billCreateDto.OrderId);

        if(existingBill != null)
        {
            _logger.LogWarning("CreateBillAsync failed: A bill already exists for Order ID {OrderId}", billCreateDto.OrderId);
            throw new BusinessRuleException($"A bill already exists for Order ID {billCreateDto.OrderId}. Cannot create duplicate bills for the same order.");
        }

        var subTotal = order.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity);

         if(billCreateDto.TaxRate < 0 || billCreateDto.TaxRate > 100)
        {
            _logger.LogWarning("CreateBillAsync failed: Invalid tax rate {TaxRate}", billCreateDto.TaxRate);
            throw new BusinessRuleException($"Invalid tax rate: {billCreateDto.TaxRate}. Tax rate must be between 0 and 100.");
        }

        if(billCreateDto.DiscountAmount < 0 || billCreateDto.DiscountAmount > subTotal)
        {
            _logger.LogWarning("CreateBillAsync failed: Invalid discount amount {DiscountAmount}", billCreateDto.DiscountAmount);
            throw new BusinessRuleException($"Invalid discount amount: {billCreateDto.DiscountAmount}. It must be between 0 and the order total amount ({subTotal}).");
        }

        if(order.Status != (int)OrderStatus.Served)
        {
            _logger.LogWarning("CreateBillAsync failed: Order status is {Status} instead of 'Served'", order.Status);
            throw new BusinessRuleException($"Cannot create bill for Order ID {billCreateDto.OrderId} because its status is not 'Served'.");
        }

        var bill = MapBillCreateDtoToEntity(order, billCreateDto, subTotal);

        int splitsCount = Math.Max(1, billCreateDto.SplitBetweenPeople);
        decimal splitAmount = Math.Round(bill.TotalAmount / splitsCount, 2);
        decimal remainder = bill.TotalAmount - (splitAmount * splitsCount);

        for (int i = 0; i < splitsCount; i++)
        {
            var split = new BillSplit
            {
                Amount = splitAmount + (i == 0 ? remainder : 0),
                Status = BillStatus.Pending
            };
            bill.Splits.Add(split);
        }

        await _billRepository.CreateBillAsync(bill);
        _logger.LogInformation("CreateBillAsync succeeded. Generated Bill ID: {BillId} for Order ID: {OrderId}", bill.Id, bill.OrderId);
        var billDto = MapBillToDto(bill);
        await _signalService.NotifyBillGeneratedAsync(order.TableId, billDto);
        return billDto;
    }

    // Retrieves a bill associated with a specific order ID.
    public async Task<BillDto> GetBillByOrderIdAsync(int orderId)
    {
        _logger.LogInformation("GetBillByOrderIdAsync called for Order ID: {OrderId}", orderId);
        var bill = await _billRepository.GetBillByOrderIdAsync(orderId);
        if (bill == null)
        {
            _logger.LogWarning("GetBillByOrderIdAsync failed: Bill for Order ID {OrderId} not found.", orderId);
            throw new NotFoundException($"Bill for Order ID {orderId} not found.");
        }
        return MapBillToDto(bill);
    }

    // Retrieves all existing bills.
    public async Task<IEnumerable<BillDto>> GetAllBillsAsync()
    {
        _logger.LogInformation("GetAllBillsAsync called");
        var bills = await _billRepository.GetAllBillsAsync();
        return bills.Select(MapBillToDto);
    }

    // Updates an existing bill.
    public async Task<BillDto> UpdateBillAsync(int id, BillCreateDto billCreateDto)
    {
        _logger.LogInformation("UpdateBillAsync started for Bill ID: {Id}", id);
        Validation.RequireNotNull(billCreateDto, nameof(billCreateDto), "Bill data is required.");
        var order = await _orderService.GetOrderByIdAsync(billCreateDto.OrderId);
        if (order == null)
        {
            _logger.LogWarning("UpdateBillAsync failed: Order with ID {OrderId} not found.", billCreateDto.OrderId);
            throw new NotFoundException($"Order with ID {billCreateDto.OrderId} not found.");
        }

        var subTotal = order.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity);

        var billEntity = new Bill
        {
            OrderId = billCreateDto.OrderId,
            SubTotal = subTotal,
            TaxRate = billCreateDto.TaxRate,
            DiscountAmount = billCreateDto.DiscountAmount,
            TotalAmount = subTotal + (subTotal * billCreateDto.TaxRate / 100m) - billCreateDto.DiscountAmount,
            Status = BillStatus.Pending
        };

        var updatedBill = await _billRepository.UpdateBillAsync(id, billEntity);
        if (updatedBill == null)
        {
            _logger.LogWarning("UpdateBillAsync failed: Bill with ID {Id} not found.", id);
            throw new NotFoundException($"Bill with ID {id} not found.");
        }

        _logger.LogInformation("UpdateBillAsync succeeded for Bill ID: {Id}", id);
        return MapBillToDto(updatedBill);
    }

    // Updates the status of a specific bill (e.g., Pending to Paid or Failed).
    public async Task<bool> UpdateBillStatusAsync(int billId, string newStatus)
    {
        _logger.LogInformation("UpdateBillStatusAsync started for Bill ID: {BillId}, new status: {Status}", billId, newStatus);
        if (!Enum.TryParse<BillStatus>(newStatus, true, out var parsedStatus))
        {
            _logger.LogWarning("UpdateBillStatusAsync failed: Invalid status name '{Status}'", newStatus);
            throw new BusinessRuleException($"Invalid bill status: {newStatus}");
        }

        var bill = await _billRepository.GetByIdAsync(billId);
        if (bill == null)
        {
            _logger.LogWarning("UpdateBillStatusAsync failed: Bill with ID {BillId} not found.", billId);
            throw new NotFoundException($"Bill with ID {billId} not found.");
        }

        // Only allow Pending -> Paid or Pending -> Failed transitions.
        if (bill.Status != BillStatus.Pending)
        {
            _logger.LogWarning("UpdateBillStatusAsync failed: Bill ID {BillId} is already in state '{Status}'", billId, bill.Status);
            throw new BusinessRuleException($"Cannot update bill status from '{bill.Status}' to '{parsedStatus}'. Bill is already in a final state.");
        }

        if (parsedStatus != BillStatus.Paid && parsedStatus != BillStatus.Failed)
        {
            _logger.LogWarning("UpdateBillStatusAsync failed: Invalid transition from Pending to {Status}", parsedStatus);
            throw new BusinessRuleException($"Invalid status transition. A pending bill can only transition to 'Paid' or 'Failed'.");
        }

        var isChanged = await _billRepository.UpdateStatusAsync(billId, parsedStatus);
        if (!isChanged)
        {
            _logger.LogWarning("UpdateBillStatusAsync failed: Bill with ID {BillId} not found during update status.", billId);
            throw new NotFoundException($"Bill with ID {billId} not found.");
        }

        if (parsedStatus == BillStatus.Paid)
        {
            _logger.LogInformation("Bill ID {BillId} is paid. Updating Order ID {OrderId} status to Completed.", billId, bill.OrderId);
            // When a bill is marked as Paid, we can also update the associated order status to Completed.
            await _orderService.UpdateOrderStatusAsync(bill.OrderId, (int)OrderStatus.Completed);

            var order = await _orderService.GetOrderByIdAsync(bill.OrderId);
            if (order != null)
            {
                var billDto = MapBillToDto(bill);
                await _signalService.NotifyBillPaidAsync(order.TableId, billDto);
                await _signalService.NotifyGuestSessionEndedAsync(order.TableId);
            }
        }

        _logger.LogInformation("UpdateBillStatusAsync completed for Bill ID {BillId}.", billId);
        return true;
    }

    public async Task<bool> PayBillSplitAsync(int splitId)
    {
        _logger.LogInformation("PayBillSplitAsync started for Split ID: {SplitId}", splitId);
        
        var split = await _billRepository.GetBillSplitByIdAsync(splitId);
        if (split == null)
        {
            _logger.LogWarning("PayBillSplitAsync failed: Split with ID {SplitId} not found.", splitId);
            throw new NotFoundException($"Bill split with ID {splitId} not found.");
        }

        if (split.Status != BillStatus.Pending)
        {
            _logger.LogWarning("PayBillSplitAsync failed: Split ID {SplitId} is already {Status}", splitId, split.Status);
            throw new BusinessRuleException($"Cannot pay split that is already in state '{split.Status}'.");
        }

        bool isUpdated = await _billRepository.UpdateBillSplitStatusAsync(splitId, BillStatus.Paid);
        if (!isUpdated)
        {
            return false;
        }

        var order = await _orderService.GetOrderByIdAsync(split.Bill.OrderId);
        if (order != null)
        {
            await _signalService.NotifyBillSplitPaidAsync(order.TableId, splitId, split.Amount);
        }

        var bill = await _billRepository.GetByIdAsync(split.BillId);
        if (bill != null && bill.Splits.All(s => s.Status == BillStatus.Paid))
        {
            _logger.LogInformation("All splits paid for Bill ID {BillId}. Updating entire bill status.", bill.Id);
            await UpdateBillStatusAsync(bill.Id, "Paid");
        }

        return true;
    }

    // Generates a PDF document for a bill.
    public async Task<byte[]> GenerateBillPdfAsync(int orderId)
    {
        _logger.LogInformation("GenerateBillPdfAsync called for Order ID: {OrderId}", orderId);
        var order = await _orderService.GetOrderByIdAsync(orderId);
        var bill = await GetBillByOrderIdAsync(orderId);

        var pdfBytes = PdfGenerator.GenerateBillPdf(order, bill);
        _logger.LogInformation("GenerateBillPdfAsync succeeded. Generated {Length} PDF bytes.", pdfBytes.Length);
        return pdfBytes;
    }

    private static BillDto MapBillToDto(Bill bill)
    {
        return new BillDto
        {
            Id = bill.Id,
            OrderId = bill.OrderId,
            SubTotal = bill.SubTotal,
            TaxRate = bill.TaxRate,
            DiscountAmount = bill.DiscountAmount,
            TotalAmount = bill.TotalAmount,
            StatusName = bill.Status.ToString(),
            CreatedAt = bill.CreatedAt,
            Splits = bill.Splits?.Select(MapBillSplitToDto).ToList() ?? []
        };
    }

    private static BillSplitDto MapBillSplitToDto(BillSplit split)
    {
        return new BillSplitDto
        {
            Id = split.Id,
            Amount = split.Amount,
            StatusName = split.Status.ToString()
        };
    }

    private static Bill MapBillCreateDtoToEntity(OrderDto orderDto, BillCreateDto billCreateDto, decimal subTotal)
    {
        return new Bill
        {
            OrderId = billCreateDto.OrderId,
            SubTotal = subTotal,
            TaxRate = billCreateDto.TaxRate,
            DiscountAmount = billCreateDto.DiscountAmount,
            TotalAmount = subTotal + (subTotal * billCreateDto.TaxRate / 100m) - billCreateDto.DiscountAmount,
            Status = Models.Enums.BillStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }
}
