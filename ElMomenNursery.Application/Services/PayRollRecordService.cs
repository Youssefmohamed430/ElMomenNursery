using System.Globalization;

namespace ElMomenNursery.Application.Services;

public sealed class PayRollRecordService(IUnitOfWork unitOfWork, ILogger<PayRollRecordService> logger,
    IPermissionService permissions, IFinanceService financeService, ISessionManager? sessions = null) : IPayRollRecordService
{
    private const int DefaultPaidDaysPerMonth = 22;
    private static readonly TimeOnly DefaultEmployeeStartTime = new(7, 30);

    public async Task<Result<PayrollRecordDto>> CalculateFinalSalary(Guid payrollRecordId)
    {
        if (!permissions.HasPermission("Payroll.Manage")) return PayrollRecordErrors.PermissionDenied;
        var record = await unitOfWork.Repository<PayrollRecord>().Query(asNoTracking: false).IgnoreQueryFilters()
            .Include(x => x.Employee).SingleOrDefaultAsync(x => x.Id == payrollRecordId && !x.IsDeleted);
        if (record is null) return PayrollRecordErrors.NotFound;
        if (record.IsPaid) return PayrollRecordErrors.AlreadyPaid;

        try
        {
            var paidDaysPerMonth = await GetPaidDaysPerMonthAsync();
            var activeStart = record.Employee.HireDate > record.PeriodStart ? record.Employee.HireDate : record.PeriodStart;
            var activeEnd = EmploymentEnd(record.Employee) is DateOnly ended && ended < record.PeriodEnd ? ended : record.PeriodEnd;
            var attendanceRows = await unitOfWork.Repository<EmployeeAttendance>().ListAsync(x =>
                x.EmployeeId == record.EmployeeId && x.AttendanceDate >= activeStart && x.AttendanceDate <= activeEnd);
            if (attendanceRows.Count == 0)
                logger.LogWarning("Payroll calculated without attendance records; payrollRecordId={PayrollRecordId}; employeeId={EmployeeId}; periodStart={PeriodStart}; periodEnd={PeriodEnd}",
                    record.Id, record.EmployeeId, record.PeriodStart, record.PeriodEnd);
            var summary = CalculateAttendanceAndProration(record.Employee, record.PeriodStart, record.PeriodEnd, attendanceRows, paidDaysPerMonth);
            var absenceDays = summary.AbsenceDays;
            record.LateMinutes = summary.LateMinutes; record.LateDeduction = summary.LateDeduction; record.ProratedSalary = summary.ProratedSalary;
            var dailySalary = record.AgreedSalary / paidDaysPerMonth;
            var salaryBeforeAdvances = Math.Max(0m, decimal.Round(summary.ProratedSalary - summary.AbsenceDays * dailySalary - summary.LateDeduction + record.Additions - record.Deductions - record.InsuranceFunds, 2, MidpointRounding.AwayFromZero));
            var advances = await unitOfWork.Repository<EmployeeAdvance>().ListAsync(x => x.EmployeeId == record.EmployeeId &&
                x.AdvanceDate <= record.PeriodEnd && x.RemainingAmount > 0);
            var outstandingAdvances = advances.Sum(x => x.RemainingAmount);
            record.AdvanceDeduction = Math.Min(salaryBeforeAdvances, outstandingAdvances);
            record.AmountPaid = salaryBeforeAdvances - record.AdvanceDeduction;
            record.IsCalculated = true;
            await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
            {
                Action = "Payroll.Calculate",
                EntityType = nameof(PayrollRecord),
                EntityId = record.Id.ToString("D"),
                UserAccountId = sessions?.CurrentUser?.UserId,
                ChangeSummary = $"period={record.PeriodStart:yyyy-MM-dd}..{record.PeriodEnd:yyyy-MM-dd};absenceDays={absenceDays};lateMinutes={record.LateMinutes};lateDeduction={record.LateDeduction};proratedSalary={record.ProratedSalary};advanceDeduction={record.AdvanceDeduction};amountPaid={record.AmountPaid}"
            });
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("Payroll record calculated; operation={Operation}; recordId={RecordId}; periodStart={PeriodStart}; periodEnd={PeriodEnd}; absenceDays={AbsenceDays}; advanceDeduction={AdvanceDeduction}; amountPaid={AmountPaid}",
                "CalculateFinalSalary", record.Id, record.PeriodStart, record.PeriodEnd, absenceDays, record.AdvanceDeduction, record.AmountPaid);
            var dto = ToDto(record, record.Employee.FullName, paidDaysPerMonth);
            dto.DailySalary = decimal.Round(dailySalary, 2, MidpointRounding.AwayFromZero);
            dto.AbsenceDays = absenceDays;
            dto.LateMinutes = record.LateMinutes; dto.LateDeduction = record.LateDeduction; dto.ProratedSalary = record.ProratedSalary;
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
        await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
        {
            Action = "Payroll.Create",
            EntityType = nameof(PayrollRecord),
            EntityId = record.Id.ToString("D"),
            UserAccountId = sessions?.CurrentUser?.UserId,
            ChangeSummary = $"period={record.PeriodStart:yyyy-MM-dd}..{record.PeriodEnd:yyyy-MM-dd};absenceDays=0;advanceDeduction={record.AdvanceDeduction};amountPaid={record.AmountPaid}"
        });
        try { await unitOfWork.SaveChangesAsync(); }
        catch (Exception ex) { logger.LogError(ex, "Payroll record creation failed"); return PayrollRecordErrors.PersistenceFailure; }
        logger.LogInformation("Payroll record created; operation={Operation}; recordId={RecordId}; periodStart={PeriodStart}; periodEnd={PeriodEnd}; absenceDays={AbsenceDays}; advanceDeduction={AdvanceDeduction}; amountPaid={AmountPaid}",
            "CreatePayRollRecord", record.Id, record.PeriodStart, record.PeriodEnd, 0, record.AdvanceDeduction, record.AmountPaid);
        return Result.Success(ToDto(record, "", await GetPaidDaysPerMonthAsync()));
    }

    public async Task<Result<PayrollBatchResultDto>> CreatePayrollRecordsForAllEmployeesAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        if (!permissions.HasPermission("Payroll.Manage")) return PayrollRecordErrors.PermissionDenied;
        return await CreatePayrollRecordsCoreAsync(startDate, endDate, cancellationToken);
    }

    public Task<Result<PayrollBatchResultDto>> CreateMonthlyPayrollRecordsAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
        => CreatePayrollRecordsCoreAsync(startDate, endDate, cancellationToken);

    private async Task<Result<PayrollBatchResultDto>> CreatePayrollRecordsCoreAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
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
                var newRecord = new PayrollRecord
                {
                    EmployeeId = employee.Id, Employee = employee, PeriodStart = startDate, PeriodEnd = endDate,
                    AgreedSalary = employee.Salary, InsuranceFunds = employee.InsuranceFunds
                };
                await unitOfWork.Repository<PayrollRecord>().AddAsync(newRecord, cancellationToken);
                await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
                {
                    Action = "Payroll.Create",
                    EntityType = nameof(PayrollRecord),
                    EntityId = newRecord.Id.ToString("D"),
                    UserAccountId = sessions?.CurrentUser?.UserId,
                    ChangeSummary = $"period={startDate:yyyy-MM-dd}..{endDate:yyyy-MM-dd};absenceDays=0;advanceDeduction={newRecord.AdvanceDeduction};amountPaid={newRecord.AmountPaid}"
                }, cancellationToken);
                logger.LogInformation("Payroll record created; operation={Operation}; recordId={RecordId}; periodStart={PeriodStart}; periodEnd={PeriodEnd}; absenceDays={AbsenceDays}; advanceDeduction={AdvanceDeduction}; amountPaid={AmountPaid}",
                    "CreatePayrollRecordsForAllEmployees", newRecord.Id, startDate, endDate, 0, newRecord.AdvanceDeduction, newRecord.AmountPaid);
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
        record.LateMinutes = 0; record.LateDeduction = 0; record.ProratedSalary = 0;
        record.IsCalculated = false;
        await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
        {
            Action = "Payroll.Update",
            EntityType = nameof(PayrollRecord),
            EntityId = record.Id.ToString("D"),
            UserAccountId = sessions?.CurrentUser?.UserId,
            ChangeSummary = $"period={record.PeriodStart:yyyy-MM-dd}..{record.PeriodEnd:yyyy-MM-dd};absenceDays=0;advanceDeduction={record.AdvanceDeduction};amountPaid={record.AmountPaid}"
        });
        try { await unitOfWork.SaveChangesAsync(); }
        catch (Exception ex) { logger.LogError(ex, "Payroll update failed; id={PayrollRecordId}", id); return PayrollRecordErrors.PersistenceFailure; }
        logger.LogInformation("Payroll record updated; operation={Operation}; recordId={RecordId}; periodStart={PeriodStart}; periodEnd={PeriodEnd}; absenceDays={AbsenceDays}; advanceDeduction={AdvanceDeduction}; amountPaid={AmountPaid}",
            "UpdatePayRollRecord", record.Id, record.PeriodStart, record.PeriodEnd, 0, record.AdvanceDeduction, record.AmountPaid);
        return Result.Success(ToDto(record, record.Employee.FullName, await GetPaidDaysPerMonthAsync()));
    }

    public async Task<Result<PayrollRecordDto>> MarkRecordAsPaid(Guid id, decimal? advanceDeduction = null)
    {
        if (!permissions.HasPermission("Payroll.Manage")) return PayrollRecordErrors.PermissionDenied;
        var record = await unitOfWork.Repository<PayrollRecord>().FirstOrDefaultAsync(x => x.Id == id, false, [nameof(PayrollRecord.Employee)]);
        if (record is null) return PayrollRecordErrors.NotFound;
        if (record.IsPaid) return PayrollRecordErrors.AlreadyPaid;
        if (!record.IsCalculated) return PayrollRecordErrors.NotCalculated;
        if (advanceDeduction is decimal requestedDeduction &&
            (requestedDeduction < 0 || decimal.Round(requestedDeduction, 2) != requestedDeduction || requestedDeduction > record.AdvanceDeduction))
            return PayrollRecordErrors.InvalidData;
        var paymentDate = record.PaymentDate ?? DateOnly.FromDateTime(DateTime.Today);
        int absenceDays = 0;
        try
        {
            await unitOfWork.BeginTransactionAsync();
            var attendanceRows = await unitOfWork.Repository<EmployeeAttendance>().ListAsync(x =>
                x.EmployeeId == record.EmployeeId &&
                x.AttendanceDate >= record.PeriodStart &&
                x.AttendanceDate <= record.PeriodEnd &&
                x.Status == AttendanceStatus.Absent);
            absenceDays = attendanceRows.Select(x => x.AttendanceDate).Distinct().Count();

            var advances = await unitOfWork.Repository<EmployeeAdvance>().ListAsync(
                x => x.EmployeeId == record.EmployeeId && x.AdvanceDate <= record.PeriodEnd && x.RemainingAmount > 0,
                asNoTracking: false);
            advances = advances.OrderBy(x => x.AdvanceDate).ThenBy(x => x.CreatedAt).ToList();
            var repaymentRepository = unitOfWork.Repository<EmployeeAdvanceRepayment>();
            var existingRepayments = await repaymentRepository.ListAsync(x => x.PayrollRecordId == record.Id,
                cancellationToken: CancellationToken.None);
            var alreadyRepaid = existingRepayments.Sum(x => x.Amount);
            var advancesAlreadyLinked = existingRepayments.Select(x => x.EmployeeAdvanceId).ToHashSet();
            var requestedTotalDeduction = advanceDeduction ?? record.AdvanceDeduction;
            var amountToAllocate = Math.Max(0, requestedTotalDeduction - alreadyRepaid);
            if (advances.Sum(x => x.RemainingAmount) < amountToAllocate)
            {
                await unitOfWork.RollbackTransactionAsync();
                return PayrollRecordErrors.AdvanceBalanceChanged;
            }
            var grossSalaryBeforeAdvances = record.AmountPaid + record.AdvanceDeduction;
            record.AdvanceDeduction = requestedTotalDeduction;
            record.AmountPaid = Math.Max(0, grossSalaryBeforeAdvances - requestedTotalDeduction);
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
            await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
            {
                Action = "Payroll.MarkPaid",
                EntityType = nameof(PayrollRecord),
                EntityId = record.Id.ToString("D"),
                UserAccountId = sessions?.CurrentUser?.UserId,
                ChangeSummary = $"period={record.PeriodStart:yyyy-MM-dd}..{record.PeriodEnd:yyyy-MM-dd};absenceDays={absenceDays};advanceDeduction={record.AdvanceDeduction};amountPaid={record.AmountPaid}"
            });
            await unitOfWork.CommitTransactionAsync();
            logger.LogInformation("Payroll record marked paid; operation={Operation}; recordId={RecordId}; periodStart={PeriodStart}; periodEnd={PeriodEnd}; absenceDays={AbsenceDays}; advanceDeduction={AdvanceDeduction}; amountPaid={AmountPaid}",
                "MarkRecordAsPaid", record.Id, record.PeriodStart, record.PeriodEnd, absenceDays, record.AdvanceDeduction, record.AmountPaid);
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
        var query = unitOfWork.Repository<PayrollRecord>().Query().IgnoreQueryFilters();
        var items = await query.Where(predicate).Include(x => x.Employee).ToListAsync();
        var employeeIds = items.Select(x => x.EmployeeId).Distinct().ToArray();
        var earliestPeriodStart = items.Count == 0 ? default : items.Min(record => record.PeriodStart);
        var latestPeriodEnd = items.Count == 0 ? default : items.Max(record => record.PeriodEnd);
        var attendanceRows = employeeIds.Length == 0 ? [] : await unitOfWork.Repository<EmployeeAttendance>().Query()
            .Where(x => employeeIds.Contains(x.EmployeeId) && x.AttendanceDate >= earliestPeriodStart && x.AttendanceDate <= latestPeriodEnd).ToListAsync();
        var paidDaysPerMonth = await GetPaidDaysPerMonthAsync();
        var result = new List<PayrollRecordDto>(items.Count);
        foreach (var item in items)
        {
            var dto = ToDto(item, item.Employee.FullName, paidDaysPerMonth);
            var attendance = attendanceRows.Where(x => x.EmployeeId == item.EmployeeId && x.AttendanceDate >= item.PeriodStart && x.AttendanceDate <= item.PeriodEnd).ToList();
            var summary = CalculateAttendanceAndProration(item.Employee, item.PeriodStart, item.PeriodEnd, attendance, paidDaysPerMonth);
            dto.AbsenceDays = summary.AbsenceDays; dto.LateMinutes = summary.LateMinutes; dto.LateDeduction = summary.LateDeduction; dto.ProratedSalary = item.ProratedSalary;
            result.Add(dto);
        }
        return Result.Success<IReadOnlyList<PayrollRecordDto>>(result);
    }

    private static PayrollRecordDto ToDto(PayrollRecord x, string name, int paidDaysPerMonth = DefaultPaidDaysPerMonth) => new()
    { Id = x.Id, EmployeeId = x.EmployeeId, EmployeeName = name, PeriodStart = x.PeriodStart, PeriodEnd = x.PeriodEnd,
      DailySalary = decimal.Round(x.AgreedSalary / paidDaysPerMonth, 2, MidpointRounding.AwayFromZero),
      AgreedSalary = x.AgreedSalary, Additions = x.Additions, Deductions = x.Deductions, AmountPaid = x.AmountPaid,
      PaymentDate = x.PaymentDate, IsPaid = x.IsPaid, IsCalculated = x.IsCalculated, Notes = x.Notes,
      InsuranceFunds = x.InsuranceFunds, AdvanceDeduction = x.AdvanceDeduction, LateMinutes = x.LateMinutes,
      LateDeduction = x.LateDeduction, ProratedSalary = x.ProratedSalary };
    private sealed record AttendancePayrollSummary(int AbsenceDays, int LateMinutes, decimal LateDeduction, decimal ProratedSalary);

    private static AttendancePayrollSummary CalculateAttendanceAndProration(Employee employee, DateOnly periodStart, DateOnly periodEnd,
        IReadOnlyCollection<EmployeeAttendance> attendance, int paidDaysPerMonth)
    {
        var start = employee.HireDate > periodStart ? employee.HireDate : periodStart;
        var employmentEnd = EmploymentEnd(employee);
        var end = employmentEnd is DateOnly lastDay && lastDay < periodEnd ? lastDay : periodEnd;
        if (end < start) return new AttendancePayrollSummary(0, 0, 0m, 0m);
        var periodDays = periodEnd.DayNumber - periodStart.DayNumber + 1;
        var employedDays = end.DayNumber - start.DayNumber + 1;
        var prorated = periodDays <= 0 ? 0m : decimal.Round(employee.Salary * employedDays / periodDays, 2, MidpointRounding.AwayFromZero);
        var byDate = attendance.GroupBy(x => x.AttendanceDate).ToDictionary(x => x.Key, x => x.OrderByDescending(row => row.UpdatedAt ?? row.CreatedAt).First());
        var scheduledStart = TimeOnly.TryParse(employee.ShiftStartTime, CultureInfo.InvariantCulture, out var parsedStart) ? parsedStart : DefaultEmployeeStartTime;
        var absences = 0;
        var lateMinutes = 0;
        var through = end < DateOnly.FromDateTime(DateTime.Today) ? end : DateOnly.FromDateTime(DateTime.Today);
        for (var date = start; date <= through; date = date.AddDays(1))
        {
            if (date.DayOfWeek == DayOfWeek.Friday) continue;
            if (!byDate.TryGetValue(date, out var row) || row.Status == AttendanceStatus.Absent) { absences++; continue; }
            if (row.Status is AttendanceStatus.Present or AttendanceStatus.Late && row.CheckInTime is DateTimeOffset checkIn)
            {
                var actual = checkIn.ToLocalTime().TimeOfDay;
                var minutes = (actual - scheduledStart.ToTimeSpan()).TotalMinutes;
                if (minutes > 0) lateMinutes += (int)Math.Ceiling(minutes);
            }
        }
        var hourly = employee.Salary / (paidDaysPerMonth * 4.5m);
        var deduction = decimal.Round(lateMinutes * hourly / 60m, 2, MidpointRounding.AwayFromZero);
        return new AttendancePayrollSummary(absences, lateMinutes, deduction, prorated);
    }

    private static DateOnly? EmploymentEnd(Employee employee) => employee.IsDeleted && employee.DeletedAt is DateTimeOffset deleted
        ? DateOnly.FromDateTime(deleted.ToLocalTime().DateTime) : null;

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
        if (!permissions.HasPermission("Employees.Advances")) return PayrollRecordErrors.PermissionDenied;
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
                UserAccountId = sessions?.CurrentUser?.UserId,
                ChangeSummary = $"employeeId={employee.Id};period={advance.AdvanceDate:yyyy-MM-dd};absenceDays=0;advanceDeduction=0;amountPaid=0;amount={advance.OriginalAmount:N2}"
            }, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            logger.LogInformation("Employee advance created; operation={Operation}; recordId={RecordId}; employeeId={EmployeeId}; period={Period}; absenceDays={AbsenceDays}; advanceDeduction={AdvanceDeduction}; amountPaid={AmountPaid}; amount={Amount}",
                "CreateEmployeeAdvance", advance.Id, employee.Id, advance.AdvanceDate, 0, 0m, 0m, advance.OriginalAmount);
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
        if (!permissions.HasPermission("Employees.Advances")) return PayrollRecordErrors.PermissionDenied;
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
