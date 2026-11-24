namespace ElMomenNursery.Application.Services;

public sealed class FinanceService(
    IUnitOfWork unitOfWork,
    ILogger<FinanceService> logger,
    IPermissionService permissions,
    ISessionManager sessions) : IFinanceService
{
    public async Task<Result<FinanceSummaryDto>> GetSummaryAsync(FinanceDateFilter filter, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Finance.View") && !Allowed("Payments.View") && !Allowed("Expenses.View"))
            return CommonErrors.PermissionDenied;

        try
        {
            // 1. Subscription Payments
            var paymentsQuery = unitOfWork.Repository<Payment>().Query().IgnoreQueryFilters()
                .Where(x => x.Status == PaymentStatus.Posted);

            if (filter.From is DateOnly fromDate)
            {
                var fromOffset = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue));
                paymentsQuery = paymentsQuery.Where(x => x.PaidAt >= fromOffset);
            }
            if (filter.To is DateOnly toDate)
            {
                var toOffset = new DateTimeOffset(toDate.ToDateTime(TimeOnly.MaxValue));
                paymentsQuery = paymentsQuery.Where(x => x.PaidAt <= toOffset);
            }

            var subIncome = (await paymentsQuery.Select(x => x.Amount).ToListAsync(cancellationToken)).Sum();

            // 2. Inventory Sales (Posted Invoices)
            var invoicesQuery = unitOfWork.Repository<Invoice>().Query().IgnoreQueryFilters()
                .Where(x => x.Status == InvoiceStatus.Posted);

            if (filter.From is DateOnly invFrom)
                invoicesQuery = invoicesQuery.Where(x => x.InvoiceDate >= invFrom);
            if (filter.To is DateOnly invTo)
                invoicesQuery = invoicesQuery.Where(x => x.InvoiceDate <= invTo);

            var invIncome = (await invoicesQuery.Select(x => x.TotalAmount).ToListAsync(cancellationToken)).Sum();

            // 3. Manual Incomes
            var manualIncomeQuery = unitOfWork.Repository<ManualIncome>().Query();

            if (filter.From is DateOnly manFrom)
                manualIncomeQuery = manualIncomeQuery.Where(x => x.IncomeDate >= manFrom);
            if (filter.To is DateOnly manTo)
                manualIncomeQuery = manualIncomeQuery.Where(x => x.IncomeDate <= manTo);

            var manIncome = (await manualIncomeQuery.Select(x => x.Amount).ToListAsync(cancellationToken)).Sum();

            // 4. Payroll Records (Paid)
            var payrollQuery = unitOfWork.Repository<PayrollRecord>().Query()
                .Where(x => x.IsPaid);

            if (filter.From is DateOnly payFrom)
                payrollQuery = payrollQuery.Where(x => (x.PaymentDate ?? x.PeriodStart) >= payFrom);
            if (filter.To is DateOnly payTo)
                payrollQuery = payrollQuery.Where(x => (x.PaymentDate ?? x.PeriodStart) <= payTo);

            var payrollExpense = (await payrollQuery.Select(x => x.AmountPaid).ToListAsync(cancellationToken)).Sum();

            // 5. Manual Expenses (Non-voided)
            var expenseQuery = unitOfWork.Repository<Expense>().Query()
                .Where(x => !x.IsVoided && x.Category != "رواتب");

            if (filter.From is DateOnly expFrom)
                expenseQuery = expenseQuery.Where(x => x.ExpenseDate >= expFrom);
            if (filter.To is DateOnly expTo)
                expenseQuery = expenseQuery.Where(x => x.ExpenseDate <= expTo);

            var manualExpense = (await expenseQuery.Select(x => x.Amount).ToListAsync(cancellationToken)).Sum();

            var summary = new FinanceSummaryDto
            {
                TotalSubscriptionIncome = subIncome,
                TotalInventorySaleIncome = invIncome,
                TotalManualIncome = manIncome,
                TotalPayrollExpense = payrollExpense,
                TotalManualExpense = manualExpense
            };

            return Result.Success(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating finance summary.");
            return Result.Failure<FinanceSummaryDto>(PersistenceErrorMapper.FromException(ex));
        }
    }

    public async Task<Result<IReadOnlyList<FinanceTransactionDto>>> GetIncomeTransactionsAsync(FinanceDateFilter filter, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Finance.View") && !Allowed("Payments.View"))
            return CommonErrors.PermissionDenied;

        try
        {
            var list = new List<FinanceTransactionDto>();

            // 1. Subscription Payments
            var paymentsQuery = unitOfWork.Repository<Payment>().Query().IgnoreQueryFilters()
                .Include(x => x.Child)
                .Include(x => x.Subscription).ThenInclude(s => s!.SubscriptionPlan)
                .Include(x => x.ReceivedByUser)
                .Where(x => x.Status == PaymentStatus.Posted);

            if (filter.From is DateOnly fromDate)
            {
                var fromOffset = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue));
                paymentsQuery = paymentsQuery.Where(x => x.PaidAt >= fromOffset);
            }
            if (filter.To is DateOnly toDate)
            {
                var toOffset = new DateTimeOffset(toDate.ToDateTime(TimeOnly.MaxValue));
                paymentsQuery = paymentsQuery.Where(x => x.PaidAt <= toOffset);
            }

            var payments = await paymentsQuery.ToListAsync(cancellationToken);
            foreach (var p in payments)
            {
                var planName = p.Subscription?.SubscriptionPlan?.Name;
                var desc = planName is not null ? $"اشتراك: {planName}" : "دفعة اشتراك";
                list.Add(new FinanceTransactionDto
                {
                    Id = p.Id,
                    Date = DateOnly.FromDateTime(p.PaidAt.LocalDateTime),
                    Type = FinanceTransactionType.SubscriptionPayment,
                    TypeLabel = "دفعة اشتراك",
                    Category = p.Child?.FullName ?? "طفل",
                    Description = desc,
                    Amount = p.Amount,
                    Reference = p.ReceiptNumber ?? p.Reference,
                    RecordedBy = p.ReceivedByUser?.FullName ?? "—",
                    CreatedAt = p.CreatedAt,
                    IsEditable = false
                });
            }

            // 2. Inventory Sales
            var invoicesQuery = unitOfWork.Repository<Invoice>().Query().IgnoreQueryFilters()
                .Include(x => x.Child)
                .Include(x => x.CreatedByUser)
                .Where(x => x.Status == InvoiceStatus.Posted);

            if (filter.From is DateOnly invFrom)
                invoicesQuery = invoicesQuery.Where(x => x.InvoiceDate >= invFrom);
            if (filter.To is DateOnly invTo)
                invoicesQuery = invoicesQuery.Where(x => x.InvoiceDate <= invTo);

            var invoices = await invoicesQuery.ToListAsync(cancellationToken);
            foreach (var inv in invoices)
            {
                var cust = inv.CustomerName ?? inv.Child?.FullName ?? "عميل نقدي";
                list.Add(new FinanceTransactionDto
                {
                    Id = inv.Id,
                    Date = inv.InvoiceDate,
                    Type = FinanceTransactionType.InventorySale,
                    TypeLabel = "مبيعات مخزن",
                    Category = cust,
                    Description = $"فاتورة بيع #{inv.InvoiceNumber}",
                    Amount = inv.TotalAmount,
                    Reference = string.IsNullOrWhiteSpace(inv.PaymentReference) ? inv.InvoiceNumber : $"{inv.InvoiceNumber} / {inv.PaymentReference}",
                    RecordedBy = inv.CreatedByUser?.FullName ?? "—",
                    CreatedAt = inv.CreatedAt,
                    IsEditable = false
                });
            }

            // 3. Manual Income
            var manualIncomeQuery = unitOfWork.Repository<ManualIncome>().Query()
                .Include(x => x.RecordedByUser)
                .AsQueryable();

            if (filter.From is DateOnly manFrom)
                manualIncomeQuery = manualIncomeQuery.Where(x => x.IncomeDate >= manFrom);
            if (filter.To is DateOnly manTo)
                manualIncomeQuery = manualIncomeQuery.Where(x => x.IncomeDate <= manTo);

            var manualIncomes = await manualIncomeQuery.ToListAsync(cancellationToken);
            foreach (var mi in manualIncomes)
            {
                list.Add(new FinanceTransactionDto
                {
                    Id = mi.Id,
                    Date = mi.IncomeDate,
                    Type = FinanceTransactionType.ManualIncome,
                    TypeLabel = "إيراد يدوي",
                    Category = mi.Source,
                    Description = mi.Description,
                    Amount = mi.Amount,
                    Reference = mi.Reference,
                    RecordedBy = mi.RecordedByUser?.FullName ?? "—",
                    CreatedAt = mi.CreatedAt,
                    IsEditable = true
                });
            }

            var sorted = list
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.CreatedAt)
                .ToList();

            return Result.Success<IReadOnlyList<FinanceTransactionDto>>(sorted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching income transactions.");
            return Result.Failure<IReadOnlyList<FinanceTransactionDto>>(PersistenceErrorMapper.FromException(ex));
        }
    }

    public async Task<Result<IReadOnlyList<FinanceTransactionDto>>> GetExpenseTransactionsAsync(FinanceDateFilter filter, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Finance.View") && !Allowed("Expenses.View"))
            return CommonErrors.PermissionDenied;

        try
        {
            var list = new List<FinanceTransactionDto>();

            // 1. Paid Payroll
            var payrollQuery = unitOfWork.Repository<PayrollRecord>().Query().IgnoreQueryFilters()
                .Where(x => !x.IsDeleted)
                .Include(x => x.Employee)
                .Where(x => x.IsPaid);

            if (filter.From is DateOnly payFrom)
                payrollQuery = payrollQuery.Where(x => (x.PaymentDate ?? x.PeriodStart) >= payFrom);
            if (filter.To is DateOnly payTo)
                payrollQuery = payrollQuery.Where(x => (x.PaymentDate ?? x.PeriodStart) <= payTo);

            var payrollRecords = await payrollQuery.ToListAsync(cancellationToken);
            foreach (var pr in payrollRecords)
            {
                var payDate = pr.PaymentDate ?? pr.PeriodStart;
                list.Add(new FinanceTransactionDto
                {
                    Id = pr.Id,
                    Date = payDate,
                    Type = FinanceTransactionType.Payroll,
                    TypeLabel = "راتب",
                    Category = "رواتب عاملين",
                    Description = $"صرف راتب: {pr.Employee.FullName} (فترة {pr.PeriodStart:MM/yyyy})",
                    Amount = pr.AmountPaid,
                    Reference = null,
                    RecordedBy = "—",
                    CreatedAt = pr.CreatedAt,
                    IsEditable = false
                });
            }

            // 2. Manual Expenses
            var expenseQuery = unitOfWork.Repository<Expense>().Query()
                .Include(x => x.RecordedByUser)
                .Where(x => !x.IsVoided && x.Category != "رواتب");

            if (filter.From is DateOnly expFrom)
                expenseQuery = expenseQuery.Where(x => x.ExpenseDate >= expFrom);
            if (filter.To is DateOnly expTo)
                expenseQuery = expenseQuery.Where(x => x.ExpenseDate <= expTo);

            var expenses = await expenseQuery.ToListAsync(cancellationToken);
            foreach (var exp in expenses)
            {
                list.Add(new FinanceTransactionDto
                {
                    Id = exp.Id,
                    Date = exp.ExpenseDate,
                    Type = FinanceTransactionType.ManualExpense,
                    TypeLabel = "مصروف خارجي",
                    Category = exp.Category,
                    Description = exp.Description,
                    Amount = exp.Amount,
                    Reference = exp.PaymentReference,
                    RecordedBy = exp.RecordedByUser?.FullName ?? "—",
                    CreatedAt = exp.CreatedAt,
                    IsEditable = exp.Category != "سلف موظفين"
                });
            }

            var sorted = list
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.CreatedAt)
                .ToList();

            return Result.Success<IReadOnlyList<FinanceTransactionDto>>(sorted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching expense transactions.");
            return Result.Failure<IReadOnlyList<FinanceTransactionDto>>(PersistenceErrorMapper.FromException(ex));
        }
    }

    public async Task<Result<FinanceTransactionDto>> CreateExpenseAsync(CreateExpenseDto dto, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Finance.ManageExpenses") && !Allowed("Expenses.Manage"))
            return CommonErrors.PermissionDenied;

        if (dto.Amount <= 0) return FinanceErrors.InvalidAmount;
        if (string.IsNullOrWhiteSpace(dto.Description)) return FinanceErrors.DescriptionRequired;
        if (string.IsNullOrWhiteSpace(dto.Category)) return FinanceErrors.CategoryRequired;

        try
        {
            var expense = new Expense
            {
                ExpenseDate = dto.ExpenseDate,
                Category = dto.Category.Trim(),
                Amount = dto.Amount,
                Description = dto.Description.Trim(),
                PaymentReference = Clean(dto.PaymentReference),
                Notes = Clean(dto.Notes),
                RecordedByUserId = sessions.CurrentUser?.UserId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await unitOfWork.Repository<Expense>().AddAsync(expense, cancellationToken);

            await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
            {
                Action = "CreateExpense",
                EntityType = nameof(Expense),
                EntityId = expense.Id.ToString("D"),
                UserAccountId = sessions.CurrentUser?.UserId,
                ChangeSummary = $"إضافة مصروف بقيمة {expense.Amount:N2} - فئة {expense.Category} - {expense.Description}"
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var result = new FinanceTransactionDto
            {
                Id = expense.Id,
                Date = expense.ExpenseDate,
                Type = FinanceTransactionType.ManualExpense,
                TypeLabel = "مصروف خارجي",
                Category = expense.Category,
                Description = expense.Description,
                Amount = expense.Amount,
                Reference = expense.PaymentReference,
                RecordedBy = sessions.CurrentUser?.FullName ?? "—",
                CreatedAt = expense.CreatedAt,
                IsEditable = true
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating expense.");
            return Result.Failure<FinanceTransactionDto>(PersistenceErrorMapper.FromException(ex));
        }
    }

    public async Task<Result<FinanceTransactionDto>> UpdateExpenseAsync(Guid id, UpdateExpenseDto dto, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Finance.ManageExpenses") && !Allowed("Expenses.Manage"))
            return CommonErrors.PermissionDenied;

        if (dto.Amount <= 0) return FinanceErrors.InvalidAmount;
        if (string.IsNullOrWhiteSpace(dto.Description)) return FinanceErrors.DescriptionRequired;
        if (string.IsNullOrWhiteSpace(dto.Category)) return FinanceErrors.CategoryRequired;

        try
        {
            var expense = await unitOfWork.Repository<Expense>()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsVoided, asNoTracking: false, cancellationToken: cancellationToken);

            if (expense is null) return FinanceErrors.ExpenseNotFound;
            if (expense.Category == "سلف موظفين") return FinanceErrors.NonManualNotEditable;

            var oldSummary = $"تاريخ={expense.ExpenseDate};فئة={expense.Category};مبلغ={expense.Amount};بيان={expense.Description}";

            expense.ExpenseDate = dto.ExpenseDate;
            expense.Category = dto.Category.Trim();
            expense.Amount = dto.Amount;
            expense.Description = dto.Description.Trim();
            expense.PaymentReference = Clean(dto.PaymentReference);
            expense.Notes = Clean(dto.Notes);
            expense.UpdatedAt = DateTimeOffset.UtcNow;

            var newSummary = $"تاريخ={expense.ExpenseDate};فئة={expense.Category};مبلغ={expense.Amount};بيان={expense.Description}";

            await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
            {
                Action = "UpdateExpense",
                EntityType = nameof(Expense),
                EntityId = expense.Id.ToString("D"),
                UserAccountId = sessions.CurrentUser?.UserId,
                ChangeSummary = $"تعديل مصروف: قبل: [{oldSummary}]; بعد: [{newSummary}]"
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var result = new FinanceTransactionDto
            {
                Id = expense.Id,
                Date = expense.ExpenseDate,
                Type = FinanceTransactionType.ManualExpense,
                TypeLabel = "مصروف خارجي",
                Category = expense.Category,
                Description = expense.Description,
                Amount = expense.Amount,
                Reference = expense.PaymentReference,
                RecordedBy = sessions.CurrentUser?.FullName ?? "—",
                CreatedAt = expense.CreatedAt,
                IsEditable = true
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating expense {Id}", id);
            return Result.Failure<FinanceTransactionDto>(PersistenceErrorMapper.FromException(ex));
        }
    }

    public async Task<Result> DeleteExpenseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Finance.ManageExpenses") && !Allowed("Expenses.Manage"))
            return CommonErrors.PermissionDenied;

        try
        {
            var expense = await unitOfWork.Repository<Expense>()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsVoided, asNoTracking: false, cancellationToken: cancellationToken);

            if (expense is null) return FinanceErrors.ExpenseNotFound;
            if (expense.Category == "سلف موظفين") return FinanceErrors.NonManualNotEditable;

            expense.IsVoided = true;
            expense.UpdatedAt = DateTimeOffset.UtcNow;

            await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
            {
                Action = "VoidExpense",
                EntityType = nameof(Expense),
                EntityId = expense.Id.ToString("D"),
                UserAccountId = sessions.CurrentUser?.UserId,
                ChangeSummary = $"إلغاء مصروف بقيمة {expense.Amount:N2} - {expense.Description}"
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error voiding expense {Id}", id);
            return Result.Failure(PersistenceErrorMapper.FromException(ex));
        }
    }

    public async Task<Result<FinanceTransactionDto>> CreateManualIncomeAsync(CreateManualIncomeDto dto, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Finance.ManageIncome"))
            return CommonErrors.PermissionDenied;

        if (dto.Amount <= 0) return FinanceErrors.InvalidAmount;
        if (string.IsNullOrWhiteSpace(dto.Description)) return FinanceErrors.DescriptionRequired;
        if (string.IsNullOrWhiteSpace(dto.Source)) return FinanceErrors.SourceRequired;

        try
        {
            var income = new ManualIncome
            {
                IncomeDate = dto.IncomeDate,
                Source = dto.Source.Trim(),
                Amount = dto.Amount,
                Description = dto.Description.Trim(),
                Reference = Clean(dto.Reference),
                Notes = Clean(dto.Notes),
                RecordedByUserId = sessions.CurrentUser?.UserId,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await unitOfWork.Repository<ManualIncome>().AddAsync(income, cancellationToken);

            await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
            {
                Action = "CreateManualIncome",
                EntityType = nameof(ManualIncome),
                EntityId = income.Id.ToString("D"),
                UserAccountId = sessions.CurrentUser?.UserId,
                ChangeSummary = $"إضافة إيراد يدوي بقيمة {income.Amount:N2} - مصدر {income.Source} - {income.Description}"
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var result = new FinanceTransactionDto
            {
                Id = income.Id,
                Date = income.IncomeDate,
                Type = FinanceTransactionType.ManualIncome,
                TypeLabel = "إيراد يدوي",
                Category = income.Source,
                Description = income.Description,
                Amount = income.Amount,
                Reference = income.Reference,
                RecordedBy = sessions.CurrentUser?.FullName ?? "—",
                CreatedAt = income.CreatedAt,
                IsEditable = true
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating manual income.");
            return Result.Failure<FinanceTransactionDto>(PersistenceErrorMapper.FromException(ex));
        }
    }

    public async Task<Result<FinanceTransactionDto>> CreateAbsenceFineAsync(Guid childId, DateOnly fineDate, string? notes = null, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Finance.ManageIncome")) return CommonErrors.PermissionDenied;
        if (childId == Guid.Empty || fineDate == default) return CommonErrors.InvalidRequest;
        try
        {
            var subscription = await unitOfWork.Repository<Subscription>().FirstOrDefaultAsync(
                x => x.ChildId == childId && x.PeriodStart <= fineDate && x.PeriodEnd >= fineDate,
                cancellationToken: cancellationToken);
            if (subscription is null) return FinanceErrors.AbsenceFineSubscriptionNotFound;
            var idempotencyKey = $"absence-fine:{childId:D}:{subscription.Id:D}";
            var existing = await unitOfWork.Repository<ManualIncome>().Query().IgnoreQueryFilters()
                .AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);
            if (existing) return FinanceErrors.AbsenceFineAlreadyExists;
            var child = await unitOfWork.Repository<Child>().FirstOrDefaultAsync(x => x.Id == childId, cancellationToken: cancellationToken);
            if (child is null) return FinanceErrors.AbsenceFineSubscriptionNotFound;
            var amount = await GetIntegerSettingAsync("Attendance.AbsenceFineAmount", 50, cancellationToken);
            var income = new ManualIncome
            {
                IncomeDate = fineDate, Source = "غرامة غياب", Amount = amount,
                Description = $"غرامة غياب - الطفل: {child.FullName}", Notes = Clean(notes),
                IdempotencyKey = idempotencyKey, RecordedByUserId = sessions.CurrentUser?.UserId,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await unitOfWork.Repository<ManualIncome>().AddAsync(income, cancellationToken);
            await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
            {
                Action = "CreateAbsenceFine", EntityType = nameof(ManualIncome), EntityId = income.Id.ToString("D"),
                UserAccountId = sessions.CurrentUser?.UserId,
                ChangeSummary = $"childId={childId:D};subscriptionId={subscription.Id:D};amount={amount}"
            }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Absence fine recorded; incomeId={IncomeId}; childId={ChildId}; subscriptionId={SubscriptionId}; amount={Amount}", income.Id, childId, subscription.Id, amount);
            return Result.Success(new FinanceTransactionDto
            {
                Id = income.Id, Date = income.IncomeDate, Type = FinanceTransactionType.ManualIncome,
                TypeLabel = "إيراد يدوي", Category = income.Source, Description = income.Description,
                Amount = income.Amount, RecordedBy = sessions.CurrentUser?.FullName ?? "—", CreatedAt = income.CreatedAt,
                IsEditable = false
            }, "تم تسجيل غرامة الغياب.");
        }
        catch (DbUpdateException ex) when (PersistenceErrorMapper.FromException(ex) == PersistenceErrors.Unique)
        {
            logger.LogWarning("Duplicate absence fine rejected by unique idempotency key; childId={ChildId}", childId);
            return FinanceErrors.AbsenceFineAlreadyExists;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Absence fine recording failed; childId={ChildId}", childId);
            return PersistenceErrorMapper.FromException(ex);
        }
    }

    public async Task<Result<FinanceTransactionDto>> UpdateManualIncomeAsync(Guid id, UpdateManualIncomeDto dto, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Finance.ManageIncome"))
            return CommonErrors.PermissionDenied;

        if (dto.Amount <= 0) return FinanceErrors.InvalidAmount;
        if (string.IsNullOrWhiteSpace(dto.Description)) return FinanceErrors.DescriptionRequired;
        if (string.IsNullOrWhiteSpace(dto.Source)) return FinanceErrors.SourceRequired;

        try
        {
            var income = await unitOfWork.Repository<ManualIncome>()
                .FirstOrDefaultAsync(x => x.Id == id, asNoTracking: false, cancellationToken: cancellationToken);

            if (income is null) return FinanceErrors.IncomeNotFound;

            var oldSummary = $"تاريخ={income.IncomeDate};مصدر={income.Source};مبلغ={income.Amount};بيان={income.Description}";

            income.IncomeDate = dto.IncomeDate;
            income.Source = dto.Source.Trim();
            income.Amount = dto.Amount;
            income.Description = dto.Description.Trim();
            income.Reference = Clean(dto.Reference);
            income.Notes = Clean(dto.Notes);
            income.UpdatedAt = DateTimeOffset.UtcNow;

            var newSummary = $"تاريخ={income.IncomeDate};مصدر={income.Source};مبلغ={income.Amount};بيان={income.Description}";

            await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
            {
                Action = "UpdateManualIncome",
                EntityType = nameof(ManualIncome),
                EntityId = income.Id.ToString("D"),
                UserAccountId = sessions.CurrentUser?.UserId,
                ChangeSummary = $"تعديل إيراد يدوي: قبل: [{oldSummary}]; بعد: [{newSummary}]"
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            var result = new FinanceTransactionDto
            {
                Id = income.Id,
                Date = income.IncomeDate,
                Type = FinanceTransactionType.ManualIncome,
                TypeLabel = "إيراد يدوي",
                Category = income.Source,
                Description = income.Description,
                Amount = income.Amount,
                Reference = income.Reference,
                RecordedBy = sessions.CurrentUser?.FullName ?? "—",
                CreatedAt = income.CreatedAt,
                IsEditable = true
            };

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating manual income {Id}", id);
            return Result.Failure<FinanceTransactionDto>(PersistenceErrorMapper.FromException(ex));
        }
    }

    public async Task<Result> DeleteManualIncomeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!Allowed("Finance.ManageIncome"))
            return CommonErrors.PermissionDenied;

        try
        {
            var income = await unitOfWork.Repository<ManualIncome>()
                .FirstOrDefaultAsync(x => x.Id == id, asNoTracking: false, cancellationToken: cancellationToken);

            if (income is null) return FinanceErrors.IncomeNotFound;

            income.IsDeleted = true;
            income.DeletedAt = DateTimeOffset.UtcNow;
            income.DeletedByUserId = sessions.CurrentUser?.UserId;

            await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
            {
                Action = "DeleteManualIncome",
                EntityType = nameof(ManualIncome),
                EntityId = income.Id.ToString("D"),
                UserAccountId = sessions.CurrentUser?.UserId,
                ChangeSummary = $"حذف إيراد يدوي بقيمة {income.Amount:N2} - {income.Description}"
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting manual income {Id}", id);
            return Result.Failure(PersistenceErrorMapper.FromException(ex));
        }
    }

    private bool Allowed(string code) => permissions.HasPermission(code);
    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private async Task<int> GetIntegerSettingAsync(string key, int defaultValue, CancellationToken cancellationToken)
    {
        var setting = await unitOfWork.Repository<AppSetting>().FirstOrDefaultAsync(x => x.Key == key,
            cancellationToken: cancellationToken);
        return setting is not null && int.TryParse(setting.Value, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var value) && value > 0 ? value : defaultValue;
    }
}
