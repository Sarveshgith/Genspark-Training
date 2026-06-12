using OrderNKitchenMS_API.Models.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

public static class PdfGenerator
{
    private static string INR(decimal amount)
    {
        return $"₹{amount:N2}";
    }

    private static List<OrderItemDto> AggregateOrderItems(
        IEnumerable<OrderItemDto> orderItems)
    {
        return orderItems
            .GroupBy(x => x.MenuItemId)
            .Select(g => new OrderItemDto
            {
                MenuItemId = g.Key,
                MenuItemName = g.First().MenuItemName,
                UnitPrice = g.First().UnitPrice,
                Quantity = g.Sum(x => x.Quantity)
            })
            .OrderBy(x => x.MenuItemName)
            .ToList();
    }

    public static byte[] GenerateBillPdf(
        OrderDto orderDto,
        BillDto billDto)
    {
        var items = AggregateOrderItems(orderDto.OrderItems);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);

                page.DefaultTextStyle(x =>
                    x.FontSize(11));

                // ---------------- HEADER ----------------

                page.Header()
                    .Column(column =>
                    {
                        column.Spacing(5);

                        column.Item()
                            .AlignCenter()
                            .Text("AMBROSIA RESTAURANT")
                            .FontSize(24)
                            .Bold();

                        column.Item()
                            .AlignCenter()
                            .Text("TAX INVOICE")
                            .FontSize(14)
                            .SemiBold();

                        column.Item()
                            .PaddingTop(10);

                        column.Item()
                            .LineHorizontal(1);
                    });

                // ---------------- CONTENT ----------------

                page.Content()
                    .PaddingVertical(15)
                    .Column(column =>
                    {
                        column.Spacing(15);

                        // Order Details Card
                        column.Item()
                            .Border(1)
                            .BorderColor(Colors.Grey.Lighten2)
                            .Padding(10)
                            .Column(details =>
                            {
                                details.Spacing(5);

                                details.Item().Text($"Order ID : #{orderDto.Id}");
                                details.Item().Text($"Table No : {orderDto.TableNumber}");
                                details.Item().Text(
                                    $"Order Date : {orderDto.CreatedAt:dd MMM yyyy hh:mm tt}");
                            });

                        // Items Table
                        column.Item()
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(5);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell()
                                        .Background(Colors.Grey.Lighten3)
                                        .Padding(8)
                                        .Text("Item")
                                        .Bold();

                                    header.Cell()
                                        .Background(Colors.Grey.Lighten3)
                                        .Padding(8)
                                        .AlignCenter()
                                        .Text("Qty")
                                        .Bold();

                                    header.Cell()
                                        .Background(Colors.Grey.Lighten3)
                                        .Padding(8)
                                        .AlignRight()
                                        .Text("Rate")
                                        .Bold();

                                    header.Cell()
                                        .Background(Colors.Grey.Lighten3)
                                        .Padding(8)
                                        .AlignRight()
                                        .Text("Amount")
                                        .Bold();
                                });

                                foreach (var item in items)
                                {
                                    table.Cell()
                                        .BorderBottom(1)
                                        .BorderColor(Colors.Grey.Lighten3)
                                        .Padding(8)
                                        .Text(item.MenuItemName);

                                    table.Cell()
                                        .BorderBottom(1)
                                        .BorderColor(Colors.Grey.Lighten3)
                                        .Padding(8)
                                        .AlignCenter()
                                        .Text(item.Quantity.ToString());

                                    table.Cell()
                                        .BorderBottom(1)
                                        .BorderColor(Colors.Grey.Lighten3)
                                        .Padding(8)
                                        .AlignRight()
                                        .Text(INR(item.UnitPrice));

                                    table.Cell()
                                        .BorderBottom(1)
                                        .BorderColor(Colors.Grey.Lighten3)
                                        .Padding(8)
                                        .AlignRight()
                                        .Text(INR(item.Quantity * item.UnitPrice));
                                }
                            });

                        decimal taxAmount =
                            billDto.SubTotal * billDto.TaxRate / 100m;

                        // Summary Section
                        column.Item()
                            .AlignRight()
                            .Width(250)
                            .Border(1)
                            .BorderColor(Colors.Grey.Lighten2)
                            .Padding(10)
                            .Column(summary =>
                            {
                                summary.Spacing(6);

                                summary.Item()
                                    .Row(row =>
                                    {
                                        row.RelativeItem()
                                            .Text("Subtotal");

                                        row.ConstantItem(100)
                                            .AlignRight()
                                            .Text(INR(billDto.SubTotal));
                                    });

                                summary.Item()
                                    .Row(row =>
                                    {
                                        row.RelativeItem()
                                            .Text($"Tax ({billDto.TaxRate}%)");

                                        row.ConstantItem(100)
                                            .AlignRight()
                                            .Text(INR(taxAmount));
                                    });

                                summary.Item()
                                    .Row(row =>
                                    {
                                        row.RelativeItem()
                                            .Text("Discount");

                                        row.ConstantItem(100)
                                            .AlignRight()
                                            .Text(INR(billDto.DiscountAmount));
                                    });

                                summary.Item()
                                    .PaddingVertical(5)
                                    .LineHorizontal(1);

                                summary.Item()
                                    .Row(row =>
                                    {
                                        row.RelativeItem()
                                            .Text("TOTAL")
                                            .Bold()
                                            .FontSize(14);

                                        row.ConstantItem(100)
                                            .AlignRight()
                                            .Text(INR(billDto.TotalAmount))
                                            .Bold()
                                            .FontSize(14);
                                    });
                            });
                    });

                // ---------------- FOOTER ----------------

                page.Footer()
                    .Column(column =>
                    {
                        column.Spacing(5);

                        column.Item()
                            .LineHorizontal(1);

                        column.Item()
                            .AlignCenter()
                            .Text("Thank you for dining with us!")
                            .Italic();

                        column.Item()
                            .AlignCenter()
                            .Text(text =>
                            {
                                text.Span("Page ");
                                text.CurrentPageNumber();
                                text.Span(" of ");
                                text.TotalPages();
                            });
                    });
            });
        })
        .GeneratePdf();
    }
}