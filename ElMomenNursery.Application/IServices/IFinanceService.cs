using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Application.IServices;

public interface IFinanceService
{
    Task<Result<FinanceSummaryDto>> GetSummaryAsync(
        FinanceDateFilter filter,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<FinanceTransactionDto>>> GetIncomeTransactionsAsync(
        FinanceDateFilter filter,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<FinanceTransactionDto>>> GetExpenseTransactionsAsync(
        FinanceDateFilter filter,
        CancellationToken cancellationToken = default);

    Task<Result<FinanceTransactionDto>> CreateExpenseAsync(
        CreateExpenseDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<FinanceTransactionDto>> UpdateExpenseAsync(
        Guid id,
        UpdateExpenseDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteExpenseAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Result<FinanceTransactionDto>> CreateManualIncomeAsync(
        CreateManualIncomeDto dto,
        CancellationToken cancellationToken = default);

    Task<Result<FinanceTransactionDto>> UpdateManualIncomeAsync(
        Guid id,
        UpdateManualIncomeDto dto,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteManualIncomeAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
