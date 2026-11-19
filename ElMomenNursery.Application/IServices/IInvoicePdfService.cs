using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Application.DTOs;

namespace ElMomenNursery.Application.IServices;

public interface IInvoicePdfService
{
    Task<Result<string>> CreateInvoiceFileAsync(InvoiceReceiptDto invoice, CancellationToken cancellationToken = default);
}
