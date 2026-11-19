using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.DTOs;

public sealed record CreateInvoiceLineDto(Guid? InventoryItemId, string Description, decimal UnitPrice, int Quantity);
public sealed record CreateInvoiceDto(string? CustomerName, Guid? ChildId, IReadOnlyList<CreateInvoiceLineDto> Lines, PaymentMethod PaymentMethod, string? Reference, string? Notes);
public sealed record InvoiceReceiptDto(Guid Id, string InvoiceNumber, DateTimeOffset CreatedAt, string? CustomerName, string? ChildName, IReadOnlyList<InvoiceReceiptLineDto> Lines, decimal TotalAmount, PaymentMethod PaymentMethod, string? Reference);
public sealed record InvoiceReceiptLineDto(string Description, decimal UnitPrice, int Quantity, decimal Total);
