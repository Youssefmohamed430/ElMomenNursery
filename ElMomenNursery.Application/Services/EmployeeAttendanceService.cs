using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ElMomenNursery.Application.Services;

public sealed class EmployeeAttendanceService(IUnitOfWork unitOfWork, ILogger<EmployeeAttendanceService> logger) : IEmployeeAttendanceService
{
    public async Task<IReadOnlyList<EmployeeDailyAttendanceDto>> GetDailyAttendanceAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var employees = await unitOfWork.Repository<Employee>().ListAsync(cancellationToken: cancellationToken);
        var attendances = await unitOfWork.Repository<EmployeeAttendance>().ListAsync(
            attendance => attendance.AttendanceDate == date,
            cancellationToken: cancellationToken);
        var byEmployee = attendances.ToDictionary(attendance => attendance.EmployeeId);

        return employees
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
            .ToList();
    }

    public async Task<IReadOnlyList<EmployeeAttendanceDto>> GetEmployeeAttendanceByRangeAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        if (startDate > endDate)
            throw new ArgumentException("Attendance start date must be before or equal to its end date.");

        var attendances = await unitOfWork.Repository<EmployeeAttendance>().ListAsync(
            attendance => attendance.EmployeeId == employeeId &&
                          attendance.AttendanceDate >= startDate &&
                          attendance.AttendanceDate <= endDate,
            includeProperties: [nameof(EmployeeAttendance.Employee)],
            cancellationToken: cancellationToken);
        return attendances.Select(ToDto).ToList();
    }

    public async Task<EmployeeAttendanceDto> MarkManualAttendanceAsync(
        CreateEmployeeAttendanceDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateAttendance(dto.AttendanceDate, dto.Status, dto.CheckInTime, dto.CheckOutTime);

        var employee = await unitOfWork.Repository<Employee>().FirstOrDefaultAsync(
            item => item.Id == dto.EmployeeId,
            cancellationToken: cancellationToken);
        if (employee is null)
            throw new KeyNotFoundException($"Employee '{dto.EmployeeId}' was not found.");

        var alreadyMarked = await unitOfWork.Repository<EmployeeAttendance>().AnyAsync(
            attendance => attendance.EmployeeId == dto.EmployeeId && attendance.AttendanceDate == dto.AttendanceDate,
            cancellationToken);
        if (alreadyMarked)
        {
            logger.LogWarning("Manual attendance rejected because a record already exists; employeeId={EmployeeId}; date={AttendanceDate}", dto.EmployeeId, dto.AttendanceDate);
            throw new InvalidOperationException("Attendance has already been recorded for this employee and date.");
        }

        var attendance = new EmployeeAttendance
        {
            EmployeeId = dto.EmployeeId,
            Employee = employee,
            AttendanceDate = dto.AttendanceDate,
            CheckInTime = dto.CheckInTime,
            CheckOutTime = dto.CheckOutTime,
            Status = dto.Status,
            EntryMethod = AttendanceEntryMethod.Manual,
            RecordedByUserId = dto.RecordedByUserId,
            Notes = NormalizeNotes(dto.Notes)
        };

        await unitOfWork.Repository<EmployeeAttendance>().AddAsync(attendance, cancellationToken);
        await AddAuditLogAsync(
            "EmployeeAttendance.Created",
            dto.RecordedByUserId,
            attendance,
            $"Status={attendance.Status}; CheckIn={FormatTime(attendance.CheckInTime)}; CheckOut={FormatTime(attendance.CheckOutTime)}");
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Recorded manual employee attendance; attendanceId={AttendanceId}; employeeId={EmployeeId}; date={AttendanceDate}", attendance.Id, dto.EmployeeId, dto.AttendanceDate);
        return ToDto(attendance);
    }

    public async Task<EmployeeAttendanceDto> UpdateEmployeeAttendanceAsync(
        Guid id,
        UpdateEmployeeAttendanceDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var attendance = await unitOfWork.Repository<EmployeeAttendance>().FirstOrDefaultAsync(
            item => item.Id == id,
            includeProperties: [nameof(EmployeeAttendance.Employee)],
            asNoTracking: false,
            cancellationToken: cancellationToken);
        if (attendance is null)
            throw new KeyNotFoundException($"Employee attendance '{id}' was not found.");

        ValidateAttendance(dto.AttendanceDate, dto.Status, dto.CheckInTime, dto.CheckOutTime);
        var duplicateDate = await unitOfWork.Repository<EmployeeAttendance>().AnyAsync(
            item => item.Id != id &&
                    item.EmployeeId == attendance.EmployeeId &&
                    item.AttendanceDate == dto.AttendanceDate,
            cancellationToken);
        if (duplicateDate)
        {
            logger.LogWarning("Attendance correction rejected because another record exists for the selected date; attendanceId={AttendanceId}; employeeId={EmployeeId}; date={AttendanceDate}", id, attendance.EmployeeId, dto.AttendanceDate);
            throw new InvalidOperationException("Attendance is already recorded for this employee and date.");
        }

        var oldValues = $"Date={attendance.AttendanceDate}; Status={attendance.Status}; CheckIn={FormatTime(attendance.CheckInTime)}; CheckOut={FormatTime(attendance.CheckOutTime)}";
        attendance.AttendanceDate = dto.AttendanceDate;
        attendance.CheckInTime = dto.CheckInTime;
        attendance.CheckOutTime = dto.CheckOutTime;
        attendance.Status = dto.Status;
        attendance.Notes = NormalizeNotes(dto.Notes);
        attendance.UpdatedAt = DateTimeOffset.UtcNow;

        unitOfWork.Repository<EmployeeAttendance>().Update(attendance);
        await AddAuditLogAsync(
            "EmployeeAttendance.Corrected",
            dto.RecordedByUserId,
            attendance,
            $"Before: {oldValues}; After: Date={attendance.AttendanceDate}; Status={attendance.Status}; CheckIn={FormatTime(attendance.CheckInTime)}; CheckOut={FormatTime(attendance.CheckOutTime)}");
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Corrected employee attendance; attendanceId={AttendanceId}; employeeId={EmployeeId}; date={AttendanceDate}", id, attendance.EmployeeId, dto.AttendanceDate);
        return ToDto(attendance);
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

    private static void ValidateAttendance(
        DateOnly date,
        AttendanceStatus status,
        DateTimeOffset? checkIn,
        DateTimeOffset? checkOut)
    {
        if (date == default)
            throw new ArgumentException("Attendance date is required.", nameof(date));
        if (!Enum.IsDefined(status))
            throw new ArgumentOutOfRangeException(nameof(status));
        if (checkIn.HasValue && checkOut.HasValue && checkOut.Value < checkIn.Value)
            throw new ArgumentException("Check-out time cannot be before check-in time.");
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
}
