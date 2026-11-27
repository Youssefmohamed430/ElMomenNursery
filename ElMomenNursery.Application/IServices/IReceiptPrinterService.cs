using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Application.IServices;

public interface IReceiptPrinterService
{
    Task<Result> PrintReceiptAsync(PaymentReceiptDto receipt, bool isCopy, CancellationToken cancellationToken = default);
}
