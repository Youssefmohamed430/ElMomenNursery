using ElMomenNursery.Application.DTOs;

namespace ElMomenNursery.Application.IServices;

public interface IReceiptPdfService
{
    Task<string> CreateReceiptFileAsync(PaymentReceiptDto receipt, bool isCopy, CancellationToken cancellationToken = default);
}
