namespace ElMomenNursery.Domain.Entities;

public sealed class LookupItem : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Category { get; set; }
    public required string Name { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
}

public static class LookupCategories
{
    public const string PaymentMethod = "PaymentMethod";
    public const string FinanceTransactionType = "FinanceType";
    public const string JobDesignation = "JobDesignation";
    public const string IncomeSource = "IncomeSource";
    public const string ExpenseCategory = "ExpenseCategory";
}
