namespace ElMomenNursery.Application.DTOs;

public sealed class FinanceSummaryDto
{
    public decimal TotalSubscriptionIncome { get; init; }
    public decimal TotalInventorySaleIncome { get; init; }
    public decimal TotalManualIncome { get; init; }
    public decimal TotalIncome => TotalSubscriptionIncome + TotalInventorySaleIncome + TotalManualIncome;

    public decimal TotalPayrollExpense { get; init; }
    public decimal TotalManualExpense { get; init; }
    public decimal TotalExpenses => TotalPayrollExpense + TotalManualExpense;

    public decimal NetBalance => TotalIncome - TotalExpenses;
}

