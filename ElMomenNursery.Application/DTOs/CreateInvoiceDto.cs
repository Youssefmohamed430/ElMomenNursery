using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.DTOs;

public sealed record CreateInvoiceLineDto(Guid? InventoryItemId, string Description, decimal UnitPrice, int Quantity);
public sealed record CreateInvoiceDto(string? CustomerName, Guid? ChildId, IReadOnlyList<CreateInvoiceLineDto> Lines, PaymentMethod PaymentMethod, string? Reference, string? Notes);
public sealed record InvoiceReceiptDto(Guid Id, string InvoiceNumber, DateTimeOffset CreatedAt, string? CustomerName, string? ChildName, IReadOnlyList<InvoiceReceiptLineDto> Lines, decimal TotalAmount, PaymentMethod PaymentMethod, string? Reference, string? IssuedByName = null);
public sealed record InvoiceReceiptLineDto(string Description, decimal UnitPrice, int Quantity, decimal Total);
public sealed record InvoiceListItemDto(Guid Id, string InvoiceNumber, DateOnly InvoiceDate, string CustomerName,
    decimal TotalAmount, InvoiceStatus Status, PaymentMethod PaymentMethod, string? PaymentReference,
    IReadOnlyList<InvoiceReceiptLineDto> Lines);
