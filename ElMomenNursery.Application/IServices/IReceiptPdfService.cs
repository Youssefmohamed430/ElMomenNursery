using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Application.IServices;

public interface IReceiptPdfService
{
    Task<Result<string>> CreateReceiptFileAsync(PaymentReceiptDto receipt, bool isCopy, CancellationToken cancellationToken = default);
}
