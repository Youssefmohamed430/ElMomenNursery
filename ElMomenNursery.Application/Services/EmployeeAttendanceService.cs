using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace ElMomenNursery.Application.Services;

public sealed class EmployeeAttendanceService(IUnitOfWork unitOfWork, ILogger<EmployeeAttendanceService> logger, IPermissionService permissions, ISessionManager sessions) : IEmployeeAttendanceService
{
    public async Task<Result<IReadOnlyList<EmployeeDailyAttendanceDto>>> GetDailyAttendanceAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "EmployeeAttendance.View", AttendanceErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;

        if (date == default || date > DateOnly.FromDateTime(DateTime.Today)) return AttendanceErrors.InvalidDate;

        var employees = await unitOfWork.Repository<Employee>().ListAsync(x => x.HireDate <= date, cancellationToken: cancellationToken);
        var attendances = await unitOfWork.Repository<EmployeeAttendance>().ListAsync(
            attendance => attendance.AttendanceDate == date,
            cancellationToken: cancellationToken);
        var byEmployee = attendances.ToDictionary(attendance => attendance.EmployeeId);

        return Result.Success<IReadOnlyList<EmployeeDailyAttendanceDto>>(employees
            .OrderBy(employee => employee.FullName)
            .Select(employee => byEmployee.TryGetValue(employee.Id, out var attendance)
                ? ToDailyDto(employee, attendance)
                : new EmployeeDailyAttendanceDto
                {
                    EmployeeId = employee.Id,
                    EmployeeName = employee.FullName,
                    JobDesignation = employee.JobDesignation,
                    AttendanceDate = date
                })
            .ToList());
    }

    public async Task<Result<IReadOnlyList<EmployeeAttendanceDto>>> GetEmployeeAttendanceByRangeAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "EmployeeAttendance.View", AttendanceErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;

        if (startDate > endDate)
            return AttendanceErrors.InvalidRange;

        var attendances = await unitOfWork.Repository<EmployeeAttendance>().ListAsync(
            attendance => attendance.EmployeeId == employeeId &&
                          attendance.AttendanceDate >= startDate &&
                          attendance.AttendanceDate <= endDate,
            includeProperties: [nameof(EmployeeAttendance.Employee)],
            cancellationToken: cancellationToken);
        return Result.Success<IReadOnlyList<EmployeeAttendanceDto>>(attendances.Select(ToDto).ToList());
    }

    public async Task<Result<IReadOnlyList<AttendanceEmployeeOptionDto>>> GetEmployeesForHistorySelectionAsync(CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "EmployeeAttendance.View", AttendanceErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;

        var employees = await unitOfWork.Repository<Employee>().ListAsync(cancellationToken: cancellationToken);
        IReadOnlyList<AttendanceEmployeeOptionDto> result = employees.OrderBy(x => x.FullName)
            .Select(x => new AttendanceEmployeeOptionDto(x.Id, x.FullName, x.JobDesignation)).ToArray();
        return Result.Success(result);
    }

    public async Task<Result<EmployeeAttendanceDto>> MarkManualAttendanceAsync(
        CreateEmployeeAttendanceDto dto,
        CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "EmployeeAttendance.Record", AttendanceErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;

        ArgumentNullException.ThrowIfNull(dto);
        var validationError = AttendanceRules.Validate(dto.AttendanceDate, dto.Status, dto.CheckInTime, dto.CheckOutTime);
        if (validationError is not null) return validationError;

        var employee = await unitOfWork.Repository<Employee>().FirstOrDefaultAsync(
            item => item.Id == dto.EmployeeId,
            asNoTracking: false,
            cancellationToken: cancellationToken);
        if (employee is null)
            return AttendanceErrors.PersonNotFound;

        var alreadyMarked = await unitOfWork.Repository<EmployeeAttendance>().AnyAsync(
            attendance => attendance.EmployeeId == dto.EmployeeId && attendance.AttendanceDate == dto.AttendanceDate,
            cancellationToken);
        if (alreadyMarked)
        {
            logger.LogWarning("Manual attendance rejected because a record already exists; employeeId={EmployeeId}", dto.EmployeeId);
            return AttendanceErrors.AlreadyRecorded;
        }

        var attendance = new EmployeeAttendance
        {
            EmployeeId = employee.Id,
            Employee = employee,
            AttendanceDate = dto.AttendanceDate,
            Status = dto.Status,
            CheckInTime = dto.CheckInTime,
            CheckOutTime = dto.CheckOutTime,
            EntryMethod = AttendanceEntryMethod.Manual,
            RecordedByUserId = sessions.CurrentUser?.UserId,
            Notes = NormalizeNotes(dto.Notes)
        };

        await unitOfWork.Repository<EmployeeAttendance>().AddAsync(attendance, cancellationToken);
        await AddAuditLogAsync(
            "EmployeeAttendance.Created",
            sessions.CurrentUser?.UserId,
            attendance,
            $"Status={attendance.Status}; CheckIn={FormatTime(attendance.CheckInTime)}; CheckOut={FormatTime(attendance.CheckOutTime)}");
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsEmployeeDateUniqueViolation(exception))
        {
            logger.LogWarning("Manual attendance raced with another insert; employeeId={EmployeeId}; constraint={ConstraintName}",
                dto.EmployeeId, "UX_EmployeeAttendances_Employee_Date");
            return AttendanceErrors.AlreadyRecorded;
        }
        catch (DbUpdateException exception)
        {
            return PersistenceErrorMapper.FromException(exception);
        }
        logger.LogInformation("Recorded manual employee attendance; attendanceId={AttendanceId}; employeeId={EmployeeId}", attendance.Id, dto.EmployeeId);
        return Result.Success(ToDto(attendance), "تم تسجيل الحضور.");
    }

    public async Task<Result<EmployeeAttendanceDto>> UpdateEmployeeAttendanceAsync(
        Guid id,
        UpdateEmployeeAttendanceDto dto,
        CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "EmployeeAttendance.Edit", AttendanceErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;

        ArgumentNullException.ThrowIfNull(dto);
        var attendance = await unitOfWork.Repository<EmployeeAttendance>().FirstOrDefaultAsync(
            item => item.Id == id,
            includeProperties: [nameof(EmployeeAttendance.Employee)],
            asNoTracking: false,
            cancellationToken: cancellationToken);
        if (attendance is null)
            return AttendanceErrors.NotFound;

        if (!AttendanceRules.HasCorrectionReason(dto.Reason)) return AttendanceErrors.CorrectionReasonRequired;
        var validationError = AttendanceRules.Validate(attendance.AttendanceDate, dto.Status, dto.CheckInTime, dto.CheckOutTime);
        if (validationError is not null) return validationError;
        var duplicateDate = await unitOfWork.Repository<EmployeeAttendance>().AnyAsync(
            item => item.Id != id &&
                    item.EmployeeId == attendance.EmployeeId &&
                    item.AttendanceDate == attendance.AttendanceDate,
            cancellationToken);
        if (duplicateDate)
        {
            logger.LogWarning("Attendance correction rejected because another record exists for the selected date; attendanceId={AttendanceId}; employeeId={EmployeeId}", id, attendance.EmployeeId);
            return AttendanceErrors.AlreadyRecorded;
        }

        var oldValues = $"Date={attendance.AttendanceDate:yyyy-MM-dd}; Status={attendance.Status}; CheckIn={FormatTime(attendance.CheckInTime)}; CheckOut={FormatTime(attendance.CheckOutTime)}";
        attendance.CheckInTime = dto.CheckInTime;
        attendance.CheckOutTime = dto.CheckOutTime;
        attendance.Status = dto.Status;
        attendance.Notes = NormalizeNotes(dto.Notes);
        attendance.UpdatedAt = DateTimeOffset.UtcNow;

        unitOfWork.Repository<EmployeeAttendance>().Update(attendance);
        await AddAuditLogAsync(
            "EmployeeAttendance.Corrected",
            sessions.CurrentUser?.UserId,
            attendance,
            $"قبل: {oldValues}; بعد: Date={attendance.AttendanceDate:yyyy-MM-dd}; Status={attendance.Status}; CheckIn={FormatTime(attendance.CheckInTime)}; CheckOut={FormatTime(attendance.CheckOutTime)}; السبب: {dto.Reason!.Trim()}; المستخدم: {sessions.CurrentUser?.FullName ?? "مستخدم غير معروف"}");
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsEmployeeDateUniqueViolation(exception))
        {
            logger.LogWarning("Attendance correction raced with another insert; attendanceId={AttendanceId}; employeeId={EmployeeId}; constraint={ConstraintName}",
                id, attendance.EmployeeId, "UX_EmployeeAttendances_Employee_Date");
            return AttendanceErrors.AlreadyRecorded;
        }
        catch (DbUpdateException exception)
        {
            return PersistenceErrorMapper.FromException(exception);
        }
        logger.LogInformation("Corrected employee attendance; attendanceId={AttendanceId}; employeeId={EmployeeId}", id, attendance.EmployeeId);
        return Result.Success(ToDto(attendance), "تم تصحيح سجل الحضور.");
    }

    public async Task<Result<EmployeeAttendanceDto>> RecordCheckOutAsync(Guid employeeId, DateOnly date, DateTimeOffset checkOutTime, CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "EmployeeAttendance.Record", AttendanceErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;

        if (date == default || date > DateOnly.FromDateTime(DateTime.Today)) return AttendanceErrors.InvalidDate;

        var attendance = await unitOfWork.Repository<EmployeeAttendance>().FirstOrDefaultAsync(
            x => x.EmployeeId == employeeId && x.AttendanceDate == date, asNoTracking: false, cancellationToken: cancellationToken);

        if (attendance is null || !attendance.CheckInTime.HasValue || attendance.CheckOutTime.HasValue)
            return AttendanceErrors.CheckOutUnavailable;

        if (checkOutTime < attendance.CheckInTime) return AttendanceErrors.CheckOutBeforeCheckIn;

        var before = $"Date={attendance.AttendanceDate:yyyy-MM-dd};Status={attendance.Status};CheckIn={FormatTime(attendance.CheckInTime)};CheckOut=—";
        attendance.CheckOutTime = checkOutTime;
        attendance.UpdatedAt = DateTimeOffset.UtcNow;
        await AddAuditLogAsync("EmployeeAttendance.CheckOut", sessions.CurrentUser?.UserId, attendance,
            $"قبل: {before}; بعد: Date={date:yyyy-MM-dd}; Status={attendance.Status}; CheckIn={FormatTime(attendance.CheckInTime)}; CheckOut={FormatTime(checkOutTime)}");
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) { return PersistenceErrorMapper.FromException(exception); }
        return Result.Success(ToDto(attendance), "تم تسجيل الانصراف.");
    }

    private async Task AddAuditLogAsync(string action, Guid? userId, EmployeeAttendance attendance, string summary)
    {
        await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
        {
            UserAccountId = userId,
            Action = action,
            EntityType = nameof(EmployeeAttendance),
            EntityId = attendance.Id.ToString("D"),
            ChangeSummary = summary
        });
    }

    private static EmployeeDailyAttendanceDto ToDailyDto(Employee employee, EmployeeAttendance attendance) => new()
    {
        EmployeeId = employee.Id,
        EmployeeName = employee.FullName,
        JobDesignation = employee.JobDesignation,
        AttendanceId = attendance.Id,
        AttendanceDate = attendance.AttendanceDate,
        Status = attendance.Status,
        CheckInTime = attendance.CheckInTime,
        CheckOutTime = attendance.CheckOutTime,
        EntryMethod = attendance.EntryMethod,
        Notes = attendance.Notes,
        RecordedByUserId = attendance.RecordedByUserId
    };

    private static EmployeeAttendanceDto ToDto(EmployeeAttendance attendance) => new()
    {
        Id = attendance.Id,
        EmployeeId = attendance.EmployeeId,
        EmployeeName = attendance.Employee?.FullName ?? string.Empty,
        AttendanceDate = attendance.AttendanceDate,
        CheckInTime = attendance.CheckInTime,
        CheckOutTime = attendance.CheckOutTime,
        Status = attendance.Status,
        EntryMethod = attendance.EntryMethod,
        RecordedByUserId = attendance.RecordedByUserId,
        Notes = attendance.Notes
    };

    private static string? NormalizeNotes(string? notes) => string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    private static string FormatTime(DateTimeOffset? value) => value?.ToString("O") ?? "—";

    private static bool IsEmployeeDateUniqueViolation(DbUpdateException exception)
    {
        var details = exception.GetBaseException().Message;
        return details.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
            && details.Contains("EmployeeAttendances", StringComparison.OrdinalIgnoreCase)
            && details.Contains("EmployeeId", StringComparison.OrdinalIgnoreCase)
            && details.Contains("AttendanceDate", StringComparison.OrdinalIgnoreCase);
    }
}
