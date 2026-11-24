using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Application.IServices;

public interface IInvoiceService
{
    Task<Result<IReadOnlyList<InvoiceListItemDto>>> GetInvoicesAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<InventoryItemDto>>> GetAvailableItemsAsync(CancellationToken cancellationToken = default);
    Task<Result<InvoiceReceiptDto>> CreateInvoiceAsync(CreateInvoiceDto dto, CancellationToken cancellationToken = default);
}
