using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ElMomenNursery.Application.Services;

public class PayRollRecordService(IUnitOfWork unitOfWork, ILogger<PayRollRecordService> logger,
    IPermissionService permissions, IFinanceService financeService) : IPayRollRecordService
{
    private const int DaysInMonth = 22; 
    public async Task<Result<PayrollRecordDto>> CalculateFinalSalary(Guid payrollRecordId)
    {
        //var permissionError = PermissionGuard.Check(permissions, "PayrollRecords.Manage", PayrollRecordErrors.PermissionDenied);
        //if (permissionError != null) return Task.FromResult(Result<PayrollRecordDto>.Failure(permissionError));

        var payrollRecord =  await unitOfWork.Repository<PayrollRecord>().FirstOrDefaultAsync(pr => pr.Id == payrollRecordId, false ,new string[] {"Employee.AttendanceRecords"});

        var salaryOfDay = payrollRecord.AgreedSalary / DaysInMonth;

        var totalAttendanceDays = payrollRecord.Employee.Attendances.Count(ar => ar.AttendanceDate >= payrollRecord.PeriodStart && ar.AttendanceDate <= payrollRecord.PeriodEnd && ar.Status == AttendanceStatus.Present);

        var totalSalary = salaryOfDay * totalAttendanceDays + payrollRecord.Additions - payrollRecord.Deductions - payrollRecord.Insurancefunds;

        payrollRecord.AmountPaid = totalSalary;

        try
        {
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("Calculated final salary for payroll record; payrollRecordId={PayrollRecordId}", payrollRecordId);
            return Result<PayrollRecordDto>.Success(payrollRecord.Adapt<PayrollRecordDto>(), PayrollRecordErrors.CalculatedFinalSalary.Description);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Final salary calculation failed; payrollRecordId={PayrollRecordId}", payrollRecordId);
            return Result<PayrollRecordDto>.Failure(PayrollRecordErrors.CalculationFailed);
        }
    }

    public async Task<Result<PayrollRecordDto>> CreatePayRollRecord(CreatePayrollRecordDto payrollRecordDto)
    {
        var newPayrollRecord = payrollRecordDto.Adapt<PayrollRecord>();

        await unitOfWork.Repository<PayrollRecord>().AddAsync(newPayrollRecord);
        await unitOfWork.SaveChangesAsync();

        return Result<PayrollRecordDto>.Success(newPayrollRecord.Adapt<PayrollRecordDto>());
    }

    public async Task<Result> DeletePayRollRecordAsync(Guid payrollRecordId)
    {
        var payrollRecord = await unitOfWork.Repository<PayrollRecord>().FirstOrDefaultAsync(pr => pr.Id == payrollRecordId);

        if (payrollRecord == null)
        {
            return Result.Failure(PayrollRecordErrors.NotFound);
        }

        unitOfWork.Repository<PayrollRecord>().Remove(payrollRecord);
        await unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<PayrollRecordDto>>> GetAllPayrollRecordsAsync()
    {
        var payrollRecords = await unitOfWork.Repository<PayrollRecord>().ListAsync(a => a.IsDeleted, true, new[] { "Employee" });

        var payrollRecordDtos = payrollRecords.Adapt<IReadOnlyList<PayrollRecordDto>>();

        return Result.Success(payrollRecordDtos);
    }

    public async Task<Result<IReadOnlyList<PayrollRecordDto>>> GetDeletedPayrollRecordsAsync(CancellationToken cancellationToken = default)
    {
        var deletedPayrollRecords = await unitOfWork.Repository<PayrollRecord>().ListAsync(a => a.IsDeleted, true, new[] { "Employee" });
        var deletedPayrollRecordDtos = deletedPayrollRecords.Adapt<IReadOnlyList<PayrollRecordDto>>();

        return Result.Success(deletedPayrollRecordDtos);
    }

    public async Task<Result<IReadOnlyList<PayrollRecordDto>>> GetPayrollRecordsByDateAsync(DateOnly startDate, DateOnly endDate)
    {
        var payrollRecords = await unitOfWork.Repository<PayrollRecord>().ListAsync(a => a.PeriodStart >= startDate && a.PeriodEnd <= endDate && !a.IsDeleted);

        var payrollRecordDtos = payrollRecords.Adapt<IReadOnlyList<PayrollRecordDto>>();

        return Result.Success(payrollRecordDtos);
    }

    public async Task<Result<IReadOnlyList<PayrollRecordDto>>> GetPayrollRecordsByEmployeeNameAsync(string employeeName)
    {
        var payrollRecords = await unitOfWork.Repository<PayrollRecord>().ListAsync(a => a.Employee.FullName == employeeName && !a.IsDeleted, false, new[] { "Employee" });

        var payrollRecordDtos = payrollRecords.Adapt<IReadOnlyList<PayrollRecordDto>>();

        return Result.Success(payrollRecordDtos);
    }

    public async Task<Result<IReadOnlyList<PayrollRecordDto>>> GetPayrollRecordsByEmployeeNameByRangeAsync(string employeeName, DateOnly startDate, DateOnly endDate)
    {
        var payrollRecords = await unitOfWork.Repository<PayrollRecord>().ListAsync(a => a.Employee.FullName == employeeName && a.PeriodStart >= startDate && a.PeriodEnd <= endDate && !a.IsDeleted, false, new[] { "Employee" });

        var payrollRecordDtos = payrollRecords.Adapt<IReadOnlyList<PayrollRecordDto>>();

        return Result.Success(payrollRecordDtos);
    }

    public async Task<Result<PayrollRecordDto>> MarkRecordAsPaid(Guid payrollRecordId)
    {
        var payrollRecord = await unitOfWork.Repository<PayrollRecord>().FirstOrDefaultAsync(pr => pr.Id == payrollRecordId);

        if (payrollRecord == null)
        {
            return Result<PayrollRecordDto>.Failure(PayrollRecordErrors.NotFound);
        }

        payrollRecord.IsPaid = true;

        var createFinanceExpenseDto = new CreateExpenseDto
        {
            Amount = payrollRecord.AmountPaid,
            Description = $"Salary payment for {payrollRecord.Employee.FullName} for the period {payrollRecord.PeriodStart} to {payrollRecord.PeriodEnd}",
            ExpenseDate = payrollRecord.PaymentDate,
            Category = "Payroll",
        };

        var financeTransactionDto = await financeService.CreateExpenseAsync(createFinanceExpenseDto);

        await unitOfWork.SaveChangesAsync();

        return Result<PayrollRecordDto>.Success(payrollRecord.Adapt<PayrollRecordDto>());
    }

    public async Task<Result<PayrollRecordDto>> UpdatePayRollRecordAsync(Guid payrollRecordId, UpdatePayrollRecordDto payrollRecordDto)
    {
        var payrollRecord = await unitOfWork.Repository<PayrollRecord>().FirstOrDefaultAsync(pr => pr.Id == payrollRecordId);

        if (payrollRecord == null)
            return Result<PayrollRecordDto>.Failure(PayrollRecordErrors.NotFound);

        payrollRecord.PeriodStart = payrollRecordDto.PeriodStart;
        payrollRecord.PeriodEnd = payrollRecordDto.PeriodEnd;
        payrollRecord.AgreedSalary = payrollRecordDto.AgreedSalary;
        payrollRecord.Additions = payrollRecordDto.Additions;
        payrollRecord.Deductions = payrollRecordDto.Deductions;
        payrollRecord.PaymentDate = payrollRecordDto.PaymentDate;
        payrollRecord.Notes = payrollRecordDto.Notes;
        payrollRecord.Insurancefunds = payrollRecordDto.Insurancefunds;

        await unitOfWork.SaveChangesAsync();

        return Result<PayrollRecordDto>.Success(payrollRecord.Adapt<PayrollRecordDto>());
    }
}
