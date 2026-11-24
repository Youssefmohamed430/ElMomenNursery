namespace ElMomenNursery.Application.Services;

public sealed class PayRollRecordService(IUnitOfWork unitOfWork, ILogger<PayRollRecordService> logger,
    IPermissionService permissions, IFinanceService financeService) : IPayRollRecordService
{
    private const int DefaultPaidDaysPerMonth = 22;

    public async Task<Result<PayrollRecordDto>> CalculateFinalSalary(Guid payrollRecordId)
    {
        if (!permissions.HasPermission("Payroll.Manage")) return PayrollRecordErrors.PermissionDenied;
        var record = await unitOfWork.Repository<PayrollRecord>().FirstOrDefaultAsync(x => x.Id == payrollRecordId,
            asNoTracking: false, includeProperties: [nameof(PayrollRecord.Employee)]);
        if (record is null) return PayrollRecordErrors.NotFound;
        if (record.IsPaid) return PayrollRecordErrors.AlreadyPaid;

        try
        {
            var paidDaysPerMonth = await GetPaidDaysPerMonthAsync();
            var attendanceRows = await unitOfWork.Repository<EmployeeAttendance>().ListAsync(x =>
                x.EmployeeId == record.EmployeeId && x.AttendanceDate >= record.PeriodStart &&
                x.AttendanceDate <= record.PeriodEnd);
            if (attendanceRows.Count == 0)
                logger.LogWarning("Payroll calculated without attendance records; payrollRecordId={PayrollRecordId}; employeeId={EmployeeId}; periodStart={PeriodStart}; periodEnd={PeriodEnd}",
                    record.Id, record.EmployeeId, record.PeriodStart, record.PeriodEnd);
            var absenceDays = attendanceRows.Where(x => x.Status == AttendanceStatus.Absent).Select(x => x.AttendanceDate).Distinct().Count();
            var dailySalary = record.AgreedSalary / paidDaysPerMonth;
            var payableDays = Math.Max(0, paidDaysPerMonth - absenceDays);
            var salaryBeforeAdvances = Math.Max(0m, decimal.Round(dailySalary * payableDays + record.Additions - record.Deductions - record.InsuranceFunds, 2, MidpointRounding.AwayFromZero));
            var advances = await unitOfWork.Repository<EmployeeAdvance>().ListAsync(x => x.EmployeeId == record.EmployeeId &&
                x.AdvanceDate <= record.PeriodEnd && x.RemainingAmount > 0);
            var outstandingAdvances = advances.Sum(x => x.RemainingAmount);
            record.AdvanceDeduction = Math.Min(salaryBeforeAdvances, outstandingAdvances);
            record.AmountPaid = salaryBeforeAdvances - record.AdvanceDeduction;
            record.IsCalculated = true;
            await unitOfWork.SaveChangesAsync();
            var dto = ToDto(record, record.Employee.FullName, paidDaysPerMonth);
            dto.DailySalary = decimal.Round(dailySalary, 2, MidpointRounding.AwayFromZero);
            dto.AbsenceDays = absenceDays;
            return Result.Success(dto, PayrollRecordErrors.CalculatedFinalSalary.Description);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Payroll calculation failed; payrollRecordId={PayrollRecordId}", payrollRecordId);
            return PayrollRecordErrors.CalculationFailed;
        }
    }

    public async Task<Result<PayrollRecordDto>> CreatePayRollRecord(CreatePayrollRecordDto dto)
    {
        if (!permissions.HasPermission("Payroll.Manage")) return PayrollRecordErrors.PermissionDenied;
        if (dto is null || dto.EmployeeId == Guid.Empty || dto.PeriodStart == default || dto.PeriodEnd < dto.PeriodStart ||
            dto.AgreedSalary < 0 || (dto.Additions ?? 0) < 0 || (dto.Deductions ?? 0) < 0 || dto.InsuranceFunds < 0 || dto.IsPaid)
            return PayrollRecordErrors.InvalidData;
        if (!await unitOfWork.Repository<Employee>().AnyAsync(x => x.Id == dto.EmployeeId)) return EmployeeErrors.NotFound;
        var duplicate = await unitOfWork.Repository<PayrollRecord>().AnyAsync(x => x.EmployeeId == dto.EmployeeId &&
            x.PeriodStart <= dto.PeriodEnd && x.PeriodEnd >= dto.PeriodStart);
        if (duplicate) return PayrollRecordErrors.InvalidData;
        var record = new PayrollRecord { EmployeeId = dto.EmployeeId, Employee = null!, PeriodStart = dto.PeriodStart,
            PeriodEnd = dto.PeriodEnd, AgreedSalary = dto.AgreedSalary, Additions = dto.Additions ?? 0,
            Deductions = dto.Deductions ?? 0, InsuranceFunds = dto.InsuranceFunds, Notes = Clean(dto.Notes) };
        await unitOfWork.Repository<PayrollRecord>().AddAsync(record);
        try { await unitOfWork.SaveChangesAsync(); }
        catch (Exception ex) { logger.LogError(ex, "Payroll record creation failed"); return PayrollRecordErrors.PersistenceFailure; }
        return Result.Success(ToDto(record, "", await GetPaidDaysPerMonthAsync()));
    }

    public async Task<Result<PayrollBatchResultDto>> CreatePayrollRecordsForAllEmployeesAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        if (!permissions.HasPermission("Payroll.Manage")) return PayrollRecordErrors.PermissionDenied;
        if (startDate == default || endDate < startDate) return PayrollRecordErrors.InvalidData;
        try
        {
            var employees = await unitOfWork.Repository<Employee>().ListAsync(cancellationToken: cancellationToken);
            var records = await unitOfWork.Repository<PayrollRecord>().ListAsync(
                x => x.PeriodStart <= endDate && x.PeriodEnd >= startDate, cancellationToken: cancellationToken);
            var existingEmployeeIds = records.Select(x => x.EmployeeId).ToHashSet();
            var eligible = employees.Where(x => !existingEmployeeIds.Contains(x.Id)).ToArray();
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            foreach (var employee in eligible)
            {
                await unitOfWork.Repository<PayrollRecord>().AddAsync(new PayrollRecord
                {
                    EmployeeId = employee.Id, Employee = employee, PeriodStart = startDate, PeriodEnd = endDate,
                    AgreedSalary = employee.Salary
                }, cancellationToken);
            }
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return Result.Success(new PayrollBatchResultDto(eligible.Length, employees.Count - eligible.Length));
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            logger.LogError(ex, "Bulk payroll record creation failed; startDate={StartDate}; endDate={EndDate}", startDate, endDate);
            return PayrollRecordErrors.PersistenceFailure;
        }
    }

    public async Task<Result<PayrollRecordDto>> UpdatePayRollRecordAsync(Guid id, UpdatePayrollRecordDto dto)
    {
        if (!permissions.HasPermission("Payroll.Manage")) return PayrollRecordErrors.PermissionDenied;
        var record = await unitOfWork.Repository<PayrollRecord>().FirstOrDefaultAsync(x => x.Id == id, false, [nameof(PayrollRecord.Employee)]);
        if (record is null) return PayrollRecordErrors.NotFound;
        if (record.IsPaid || dto.PeriodStart == default || dto.PeriodEnd < dto.PeriodStart || dto.AgreedSalary < 0 ||
            dto.Additions < 0 || dto.Deductions < 0 || dto.InsuranceFunds < 0) return PayrollRecordErrors.InvalidData;
        var overlaps = await unitOfWork.Repository<PayrollRecord>().AnyAsync(x => x.Id != id && x.EmployeeId == record.EmployeeId &&
            x.PeriodStart <= dto.PeriodEnd && x.PeriodEnd >= dto.PeriodStart);
        if (overlaps) return PayrollRecordErrors.InvalidData;
        record.PeriodStart = dto.PeriodStart; record.PeriodEnd = dto.PeriodEnd; record.AgreedSalary = dto.AgreedSalary;
        record.Additions = dto.Additions; record.Deductions = dto.Deductions; record.InsuranceFunds = dto.InsuranceFunds;
        record.AmountPaid = 0; record.PaymentDate = dto.PaymentDate; record.Notes = Clean(dto.Notes);
        record.AdvanceDeduction = 0;
        record.IsCalculated = false;
        try { await unitOfWork.SaveChangesAsync(); }
        catch (Exception ex) { logger.LogError(ex, "Payroll update failed; id={PayrollRecordId}", id); return PayrollRecordErrors.PersistenceFailure; }
        return Result.Success(ToDto(record, record.Employee.FullName, await GetPaidDaysPerMonthAsync()));
    }

    public async Task<Result<PayrollRecordDto>> MarkRecordAsPaid(Guid id)
    {
        if (!permissions.HasPermission("Payroll.Manage")) return PayrollRecordErrors.PermissionDenied;
        var record = await unitOfWork.Repository<PayrollRecord>().FirstOrDefaultAsync(x => x.Id == id, false, [nameof(PayrollRecord.Employee)]);
        if (record is null) return PayrollRecordErrors.NotFound;
        if (record.IsPaid) return PayrollRecordErrors.AlreadyPaid;
        if (!record.IsCalculated) return PayrollRecordErrors.NotCalculated;
        var paymentDate = record.PaymentDate ?? DateOnly.FromDateTime(DateTime.Today);
        try
        {
            await unitOfWork.BeginTransactionAsync();
            var advances = await unitOfWork.Repository<EmployeeAdvance>().ListAsync(
                x => x.EmployeeId == record.EmployeeId && x.AdvanceDate <= record.PeriodEnd && x.RemainingAmount > 0,
                asNoTracking: false);
            advances = advances.OrderBy(x => x.AdvanceDate).ThenBy(x => x.CreatedAt).ToList();
            var repaymentRepository = unitOfWork.Repository<EmployeeAdvanceRepayment>();
            var existingRepayments = await repaymentRepository.ListAsync(x => x.PayrollRecordId == record.Id,
                cancellationToken: CancellationToken.None);
            var alreadyRepaid = existingRepayments.Sum(x => x.Amount);
            var advancesAlreadyLinked = existingRepayments.Select(x => x.EmployeeAdvanceId).ToHashSet();
            var amountToAllocate = Math.Max(0, record.AdvanceDeduction - alreadyRepaid);
            if (advances.Sum(x => x.RemainingAmount) < amountToAllocate)
            {
                await unitOfWork.RollbackTransactionAsync();
                return PayrollRecordErrors.AdvanceBalanceChanged;
            }
            var expenseResult = record.AmountPaid > 0
                ? await financeService.CreateExpenseAsync(new CreateExpenseDto
                {
                    Amount = record.AmountPaid, Description = $"صرف راتب {record.Employee.FullName} عن الفترة {record.PeriodStart:dd/MM/yyyy} إلى {record.PeriodEnd:dd/MM/yyyy}",
                    ExpenseDate = paymentDate, Category = "رواتب"
                })
                : null;
            if (expenseResult?.IsFailure == true)
            {
                await unitOfWork.RollbackTransactionAsync();
                return expenseResult.Error;
            }
            foreach (var advance in advances)
            {
                if (advancesAlreadyLinked.Contains(advance.Id)) continue;
                if (amountToAllocate <= 0) break;
                var repayment = Math.Min(advance.RemainingAmount, amountToAllocate);
                if (repayment <= 0) continue;
                advance.RemainingAmount -= repayment;
                amountToAllocate -= repayment;
                await unitOfWork.Repository<EmployeeAdvanceRepayment>().AddAsync(new EmployeeAdvanceRepayment
                {
                    EmployeeAdvanceId = advance.Id, EmployeeAdvance = advance,
                    PayrollRecordId = record.Id, PayrollRecord = record,
                    Amount = repayment, RepaymentDate = paymentDate
                });
            }
            record.IsPaid = true; record.PaymentDate = paymentDate;
            await unitOfWork.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync();
            logger.LogError(ex, "Payroll payment posting failed; id={PayrollRecordId}", id);
            return PayrollRecordErrors.PersistenceFailure;
        }
        return Result.Success(ToDto(record, record.Employee.FullName, await GetPaidDaysPerMonthAsync()));
    }

    public async Task<Result> DeletePayRollRecordAsync(Guid id)
    {
        if (!permissions.HasPermission("Payroll.Manage")) return PayrollRecordErrors.PermissionDenied;
        var record = await unitOfWork.Repository<PayrollRecord>().FirstOrDefaultAsync(x => x.Id == id, false);
        if (record is null) return PayrollRecordErrors.NotFound;
        if (record.IsPaid) return PayrollRecordErrors.InvalidData;
        unitOfWork.Repository<PayrollRecord>().Remove(record);
        await unitOfWork.SaveChangesAsync(); return Result.Success();
    }

    public Task<Result<IReadOnlyList<PayrollRecordDto>>> GetAllPayrollRecordsAsync() => GetRecordsAsync(x => !x.IsDeleted);
    public Task<Result<IReadOnlyList<PayrollRecordDto>>> GetDeletedPayrollRecordsAsync(CancellationToken cancellationToken = default) => GetRecordsAsync(x => x.IsDeleted, includeDeleted: true);
    public Task<Result<IReadOnlyList<PayrollRecordDto>>> GetPayrollRecordsByDateAsync(DateOnly start, DateOnly end) =>
        GetRecordsAsync(x => !x.IsDeleted && x.PeriodStart <= end && x.PeriodEnd >= start);
    public Task<Result<IReadOnlyList<PayrollRecordDto>>> GetPayrollRecordsByEmployeeNameAsync(string name) =>
        GetRecordsAsync(x => !x.IsDeleted && x.Employee.FullName == name);
    public Task<Result<IReadOnlyList<PayrollRecordDto>>> GetPayrollRecordsByEmployeeNameByRangeAsync(string name, DateOnly start, DateOnly end) =>
        GetRecordsAsync(x => !x.IsDeleted && x.Employee.FullName == name && x.PeriodStart <= end && x.PeriodEnd >= start);

    private async Task<Result<IReadOnlyList<PayrollRecordDto>>> GetRecordsAsync(System.Linq.Expressions.Expression<Func<PayrollRecord, bool>> predicate, bool includeDeleted = false)
    {
        if (!permissions.HasPermission("Payroll.Manage")) return PayrollRecordErrors.PermissionDenied;
        var query = unitOfWork.Repository<PayrollRecord>().Query();
        if (includeDeleted) query = query.IgnoreQueryFilters();
        var items = await query.Where(predicate).Include(x => x.Employee).ToListAsync();
        var employeeIds = items.Select(x => x.EmployeeId).Distinct().ToArray();
        var earliestPeriodStart = items.Count == 0 ? default : items.Min(record => record.PeriodStart);
        var latestPeriodEnd = items.Count == 0 ? default : items.Max(record => record.PeriodEnd);
        var attendanceRows = employeeIds.Length == 0 ? [] : await unitOfWork.Repository<EmployeeAttendance>().Query()
            .Where(x => employeeIds.Contains(x.EmployeeId)
                && x.AttendanceDate >= earliestPeriodStart
                && x.AttendanceDate <= latestPeriodEnd
                && x.Status == AttendanceStatus.Absent)
            .Select(x => new { x.EmployeeId, x.AttendanceDate }).ToListAsync();
        var paidDaysPerMonth = await GetPaidDaysPerMonthAsync();
        var result = new List<PayrollRecordDto>(items.Count);
        foreach (var item in items)
        {
            var dto = ToDto(item, item.Employee.FullName, paidDaysPerMonth);
            dto.AbsenceDays = attendanceRows.Where(x => x.EmployeeId == item.EmployeeId
                && x.AttendanceDate >= item.PeriodStart && x.AttendanceDate <= item.PeriodEnd)
                .Select(x => x.AttendanceDate).Distinct().Count();
            result.Add(dto);
        }
        return Result.Success<IReadOnlyList<PayrollRecordDto>>(result);
    }

    private static PayrollRecordDto ToDto(PayrollRecord x, string name, int paidDaysPerMonth = DefaultPaidDaysPerMonth) => new()
    { Id = x.Id, EmployeeId = x.EmployeeId, EmployeeName = name, PeriodStart = x.PeriodStart, PeriodEnd = x.PeriodEnd,
      DailySalary = decimal.Round(x.AgreedSalary / paidDaysPerMonth, 2, MidpointRounding.AwayFromZero),
      AgreedSalary = x.AgreedSalary, Additions = x.Additions, Deductions = x.Deductions, AmountPaid = x.AmountPaid,
      PaymentDate = x.PaymentDate, IsPaid = x.IsPaid, IsCalculated = x.IsCalculated, Notes = x.Notes,
      InsuranceFunds = x.InsuranceFunds, AdvanceDeduction = x.AdvanceDeduction };
    private async Task<int> GetPaidDaysPerMonthAsync(CancellationToken cancellationToken = default)
    {
        var setting = await unitOfWork.Repository<AppSetting>().FirstOrDefaultAsync(
            x => x.Key == "Payroll.PaidDaysPerMonth", cancellationToken: cancellationToken);
        return setting is not null && int.TryParse(setting.Value, out var days) && days > 0
            ? days : DefaultPaidDaysPerMonth;
    }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public async Task<Result<EmployeeAdvanceDto>> CreateEmployeeAdvanceAsync(CreateEmployeeAdvanceDto dto, CancellationToken cancellationToken = default)
    {
        if (!permissions.HasPermission("Payroll.Manage")) return PayrollRecordErrors.PermissionDenied;
        if (dto is null || dto.EmployeeId == Guid.Empty || dto.Amount <= 0 || decimal.Round(dto.Amount, 2) <= 0 || dto.AdvanceDate == default)
            return PayrollRecordErrors.InvalidAdvance;
        var employee = await unitOfWork.Repository<Employee>().FirstOrDefaultAsync(x => x.Id == dto.EmployeeId,
            asNoTracking: false, cancellationToken: cancellationToken);
        if (employee is null) return EmployeeErrors.NotFound;
        var advance = new EmployeeAdvance
        {
            EmployeeId = employee.Id, Employee = employee, OriginalAmount = decimal.Round(dto.Amount, 2),
            RemainingAmount = decimal.Round(dto.Amount, 2), AdvanceDate = dto.AdvanceDate, Notes = Clean(dto.Notes)
        };
        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            var expense = await financeService.CreateExpenseAsync(new CreateExpenseDto
            {
                Amount = advance.OriginalAmount, ExpenseDate = advance.AdvanceDate,
                Category = "سلف موظفين", Description = $"سلفة للموظف {employee.FullName}"
            }, cancellationToken);
            if (expense.IsFailure)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return expense.Error;
            }
            await unitOfWork.Repository<EmployeeAdvance>().AddAsync(advance, cancellationToken);
            await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
            {
                Action = "CreateEmployeeAdvance", EntityType = nameof(EmployeeAdvance), EntityId = advance.Id.ToString("D"),
                ChangeSummary = $"تسجيل سلفة للموظف {employee.FullName} بقيمة {advance.OriginalAmount:N2}"
            }, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Employee advance creation failed; employeeId={EmployeeId}", employee.Id);
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            return PayrollRecordErrors.PersistenceFailure;
        }
        return Result.Success(ToAdvanceDto(advance));
    }

    public async Task<Result<IReadOnlyList<EmployeeAdvanceDto>>> GetEmployeeAdvancesAsync(Guid? employeeId = null, CancellationToken cancellationToken = default)
    {
        if (!permissions.HasPermission("Payroll.Manage")) return PayrollRecordErrors.PermissionDenied;
        var advances = await unitOfWork.Repository<EmployeeAdvance>().ListAsync(
            includeProperties: [nameof(EmployeeAdvance.Employee)], cancellationToken: cancellationToken);
        return Result.Success<IReadOnlyList<EmployeeAdvanceDto>>(advances
            .Where(x => !employeeId.HasValue || x.EmployeeId == employeeId.Value)
            .OrderByDescending(x => x.AdvanceDate).Select(ToAdvanceDto).ToArray());
    }

    private static EmployeeAdvanceDto ToAdvanceDto(EmployeeAdvance x) => new()
    {
        Id = x.Id, EmployeeId = x.EmployeeId, EmployeeName = x.Employee.FullName,
        OriginalAmount = x.OriginalAmount, RemainingAmount = x.RemainingAmount, AdvanceDate = x.AdvanceDate, Notes = x.Notes
    };
}
