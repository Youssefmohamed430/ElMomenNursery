namespace ElMomenNursery.Application.Services;

public sealed class ChildAttendanceService(IUnitOfWork unitOfWork, ILogger<ChildAttendanceService> logger,
    IPermissionService permissions, ISessionManager sessions) : IChildAttendanceService
{
    public async Task<Result<IReadOnlyList<ChildDailyAttendanceDto>>> GetDailyAttendanceAsync(ChildDailyAttendanceQuery query, CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Attendance.View", AttendanceErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;

        if (query.Date == default || query.Date > DateOnly.FromDateTime(DateTime.Today)) return AttendanceErrors.InvalidDate;

        var children = await unitOfWork.Repository<Child>().ListAsync(x => x.EnrollmentDate <= query.Date, cancellationToken: cancellationToken);

        var childIds = children.Select(x => x.Id).ToArray();

        var enrollments = await unitOfWork.Repository<ChildClassEnrollment>().ListAsync(
            x => childIds.Contains(x.ChildId) && x.StartDate <= query.Date && (x.EndDate == null || x.EndDate >= query.Date)
                 && (!query.ClassroomId.HasValue || x.ClassroomId == query.ClassroomId.Value),
            includeProperties: [nameof(ChildClassEnrollment.Classroom)], cancellationToken: cancellationToken);

        if (query.ClassroomId.HasValue)
            children = children.Where(c => enrollments.Any(e => e.ChildId == c.Id)).ToList();

        var attendance = await unitOfWork.Repository<ChildAttendance>().ListAsync(
            x => childIds.Contains(x.ChildId) && x.AttendanceDate == query.Date, cancellationToken: cancellationToken);

        var enrollmentsByChild = enrollments.GroupBy(x => x.ChildId).ToDictionary(g => g.Key,
            g => string.Join("، ", g.Select(x => x.Classroom.Name).Distinct().Order(StringComparer.CurrentCulture)));

        var attendanceByChild = attendance.ToDictionary(x => x.ChildId);

        IReadOnlyList<ChildDailyAttendanceDto> rows = children.OrderBy(x => x.FullName).Select(child =>
        {
            attendanceByChild.TryGetValue(child.Id, out var record);
            return new ChildDailyAttendanceDto
            {
                ChildId = child.Id,
                ChildName = child.FullName,
                ClassroomNames = enrollmentsByChild.GetValueOrDefault(child.Id, "—"),
                Date = query.Date,
                AttendanceId = record?.Id,
                Status = record?.Status,
                CheckIn = record?.CheckInTime,
                CheckOut = record?.CheckOutTime,
                EntryMethod = record?.EntryMethod,
                Notes = record?.Notes
            };
        }).ToArray();

        return Result.Success(rows);
    }

    public async Task<Result<IReadOnlyList<ChildAttendanceDto>>> GetChildAttendanceByRangeAsync(Guid childId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Attendance.View", AttendanceErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;

        if (start > end) return AttendanceErrors.InvalidRange;
        var rows = await unitOfWork.Repository<ChildAttendance>().ListAsync(x => x.ChildId == childId && x.AttendanceDate >= start && x.AttendanceDate <= end, cancellationToken: cancellationToken);
        return Result.Success<IReadOnlyList<ChildAttendanceDto>>(rows.Select(ToDto).ToArray());
    }

    public async Task<Result<IReadOnlyList<AttendanceChildOptionDto>>> GetChildrenForHistorySelectionAsync(CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Attendance.View", AttendanceErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;

        var children = await unitOfWork.Repository<Child>().ListAsync(cancellationToken: cancellationToken);
        IReadOnlyList<AttendanceChildOptionDto> result = children.OrderBy(x => x.FullName)
            .Select(x => new AttendanceChildOptionDto(x.Id, x.FullName)).ToArray();

        return Result.Success(result);
    }

    public async Task<Result<IReadOnlyList<ClassroomFilterOptionDto>>> GetClassroomsForFilterAsync(CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Attendance.View", AttendanceErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;

        var classrooms = await unitOfWork.Repository<Classroom>().ListAsync(x => x.IsActive, cancellationToken: cancellationToken);

        return Result.Success<IReadOnlyList<ClassroomFilterOptionDto>>(classrooms.OrderBy(x => x.Name)
            .Select(x => new ClassroomFilterOptionDto(x.Id, x.Name)).ToArray());
    }

    public async Task<Result<ChildAttendanceDto>> MarkManualAttendanceAsync(CreateChildAttendanceDto dto, CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Attendance.Record", AttendanceErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;

        var error = AttendanceRules.Validate(dto.Date, dto.Status, dto.CheckIn, dto.CheckOut);
        if (error is not null) return error;

        var child = await unitOfWork.Repository<Child>().FirstOrDefaultAsync(x => x.Id == dto.ChildId, asNoTracking: false, new string[] { "Subscriptions" }, cancellationToken: cancellationToken);
        if (child is null || child.EnrollmentDate > dto.Date) return AttendanceErrors.PersonNotFound;

        if (await unitOfWork.Repository<ChildAttendance>().AnyAsync(x => x.ChildId == dto.ChildId && x.AttendanceDate == dto.Date, cancellationToken)) return AttendanceErrors.AlreadyRecorded;

        var item = new ChildAttendance
        {
            ChildId = child.Id,
            Child = child,
            AttendanceDate = dto.Date,
            Status = dto.Status,
            CheckInTime = dto.CheckIn,
            CheckOutTime = dto.CheckOut,
            EntryMethod = AttendanceEntryMethod.Manual,
            Notes = Clean(dto.Notes),
            RecordedByUserId = sessions.CurrentUser?.UserId
        };

        await unitOfWork.Repository<ChildAttendance>().AddAsync(item, cancellationToken);
        await AddAuditAsync(item, "ChildAttendance.Created", "—", Values(item), null, cancellationToken);

        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) when (IsDuplicate(ex)) { logger.LogWarning("Duplicate child attendance rejected; childId={ChildId}", child.Id); return AttendanceErrors.AlreadyRecorded; }
        catch (DbUpdateException ex) { logger.LogWarning(ex, "Child attendance persistence failed; errorCode={ErrorCode}", PersistenceErrorMapper.FromException(ex).Code); return PersistenceErrorMapper.FromException(ex); }

        var alert = await GetSubscriptionAndAbsenceAlertAsync(child.Id, dto.Date, cancellationToken);

        return Result.Success(ToDto(item) with { Alert = alert }, alert ?? "تم تسجيل الحضور.");
    } 
    
    public async Task<Result<ChildAttendanceDto>> UpdateChildAttendanceAsync(Guid id, UpdateChildAttendanceDto dto, CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Attendance.Edit", AttendanceErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;

        if (!AttendanceRules.HasCorrectionReason(dto.Reason)) return AttendanceErrors.CorrectionReasonRequired;

        var item = await unitOfWork.Repository<ChildAttendance>().FirstOrDefaultAsync(x => x.Id == id, asNoTracking: false, cancellationToken: cancellationToken);
        if (item is null) return AttendanceErrors.NotFound;

        var error = AttendanceRules.Validate(item.AttendanceDate, dto.Status, dto.CheckIn, dto.CheckOut);
        if (error is not null) return error;

        var before = Values(item);
        item.Status = dto.Status; item.CheckInTime = dto.CheckIn; item.CheckOutTime = dto.CheckOut;
        item.Notes = Clean(dto.Notes); item.UpdatedAt = DateTimeOffset.UtcNow;

        await AddAuditAsync(item, "ChildAttendance.Corrected", before, Values(item), dto.Reason!.Trim(), cancellationToken);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) { return PersistenceErrorMapper.FromException(ex); }
        return Result.Success(ToDto(item), "تم تصحيح سجل الحضور.");
    }

    public async Task<Result<ChildAttendanceDto>> RecordCheckOutAsync(Guid childId, DateOnly date, DateTimeOffset checkOut, CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Attendance.Record", AttendanceErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;

        if (date == default || date > DateOnly.FromDateTime(DateTime.Today)) return AttendanceErrors.InvalidDate;

        var item = await unitOfWork.Repository<ChildAttendance>().FirstOrDefaultAsync(x => x.ChildId == childId && x.AttendanceDate == date, asNoTracking: false, cancellationToken: cancellationToken);
        if (item is null || !item.CheckInTime.HasValue || item.CheckOutTime.HasValue) return AttendanceErrors.CheckOutUnavailable;

        if (checkOut < item.CheckInTime) return AttendanceErrors.CheckOutBeforeCheckIn;

        var before = Values(item);

        item.CheckOutTime = checkOut; item.UpdatedAt = DateTimeOffset.UtcNow;

        await AddAuditAsync(item, "ChildAttendance.CheckOut", before, Values(item), null, cancellationToken);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) { return PersistenceErrorMapper.FromException(ex); }

        return Result.Success(ToDto(item), "تم تسجيل الانصراف.");
    }

    public async Task<Result<int>> MarkAllPresentAsync(DateOnly date, Guid? classroomId, CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Attendance.Record", AttendanceErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;

        if (date == default || date > DateOnly.FromDateTime(DateTime.Today)) return AttendanceErrors.InvalidDate;

        var children = await unitOfWork.Repository<Child>().ListAsync(x => x.EnrollmentDate <= date, asNoTracking: false, cancellationToken: cancellationToken);
        var ids = children.Select(x => x.Id).ToArray();

        if (classroomId.HasValue)
        {
            var enrollment = await unitOfWork.Repository<ChildClassEnrollment>().ListAsync(x => ids.Contains(x.ChildId)
                && x.ClassroomId == classroomId && x.StartDate <= date && (x.EndDate == null || x.EndDate >= date), cancellationToken: cancellationToken);
            var enrolledIds = enrollment.Select(x => x.ChildId).ToHashSet();
            children = children.Where(x => enrolledIds.Contains(x.Id)).ToList();
        }
        var present = await unitOfWork.Repository<ChildAttendance>().ListAsync(x => ids.Contains(x.ChildId) && x.AttendanceDate == date, cancellationToken: cancellationToken);
        var existing = present.Select(x => x.ChildId).ToHashSet();
        var actorId = sessions.CurrentUser?.UserId;
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var child in children.Where(x => !existing.Contains(x.Id)))
            {
                var item = new ChildAttendance { ChildId = child.Id, Child = child, AttendanceDate = date,
                    Status = AttendanceStatus.Present, CheckInTime = DateTimeOffset.Now,
                    EntryMethod = AttendanceEntryMethod.Manual, RecordedByUserId = actorId };
                await unitOfWork.Repository<ChildAttendance>().AddAsync(item, cancellationToken);
                await AddAuditAsync(item, "ChildAttendance.Created", "—", Values(item), null, cancellationToken);
            }
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return Result.Success(children.Count(x => !existing.Contains(x.Id)), "تم تسجيل حضور الأطفال غير المسجلين.");
        }
        catch (DbUpdateException ex) when (IsDuplicate(ex))
        {
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            return AttendanceErrors.AlreadyRecorded;
        }
        catch (DbUpdateException ex)
        {
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            return PersistenceErrorMapper.FromException(ex);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task AddAuditAsync(ChildAttendance item, string action, string before, string after, string? reason, CancellationToken ct)
    {
        var actor = sessions.CurrentUser?.FullName ?? "مستخدم غير معروف";
        var summary = $"قبل: {before}; بعد: {after}" + (reason is null ? string.Empty : $"; السبب: {reason}; المستخدم: {actor}");
        await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog { UserAccountId = sessions.CurrentUser?.UserId,
            Action = action, EntityType = nameof(ChildAttendance), EntityId = item.Id.ToString("D"), ChangeSummary = summary }, ct);
    }

    private async Task<string?> GetSubscriptionAndAbsenceAlertAsync(Guid childId, DateOnly attendanceDate, CancellationToken ct)
    {
        try
        {
            var messages = new List<string>();
        var subscription = await unitOfWork.Repository<Subscription>().FirstOrDefaultAsync(
            x => x.ChildId == childId && x.PeriodStart <= attendanceDate && x.PeriodEnd >= attendanceDate,
            cancellationToken: ct);
        if (subscription is not null && subscription.Status is not (SubscriptionStatus.Cancelled or SubscriptionStatus.Waived)
            && attendanceDate >= subscription.PeriodStart.AddDays(await GetIntegerSettingAsync("Attendance.SubscriptionAlertAfterDays", 3, ct)))
        {
            var paid = (await unitOfWork.Repository<PaymentAllocation>().Query()
                .Where(x => x.SubscriptionId == subscription.Id && x.Payment.Status == PaymentStatus.Posted)
                .Select(x => x.AmountApplied).ToListAsync(ct)).Sum();
            if (paid < subscription.AmountDue)
                messages.Add($"تنبيه: اشتراك الطفل غير مسدد بالكامل بعد مرور 3 أيام من بدايته. المتبقي {subscription.AmountDue - paid:0.##} جنيه.");
        }

        if (subscription is not null)
        {
            var absentDays = await unitOfWork.Repository<ChildAttendance>().Query()
                .CountAsync(x => x.ChildId == childId && x.AttendanceDate >= subscription.PeriodStart
                    && x.AttendanceDate <= attendanceDate && x.Status == AttendanceStatus.Absent, ct);
            var fineAfterDays = await GetIntegerSettingAsync("Attendance.AbsenceFineAfterDays", 15, ct);
            var fineAmount = await GetIntegerSettingAsync("Attendance.AbsenceFineAmount", 50, ct);
            if (absentDays >= fineAfterDays)
                messages.Add($"تنبيه: تم تسجيل غياب الطفل {fineAfterDays} يومًا منذ بداية الاشتراك. الغرامة المستحقة {fineAmount} جنيهًا.");
        }

            return messages.Count == 0 ? null : string.Join(Environment.NewLine, messages);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to compute attendance alerts; operation={Operation}; childId={ChildId}; attendanceDate={AttendanceDate}",
                "GetSubscriptionAndAbsenceAlert", childId, attendanceDate);
            return null;
        }
    }

    private static string Values(ChildAttendance x) => $"Date={x.AttendanceDate:yyyy-MM-dd};Status={x.Status};CheckIn={x.CheckInTime?.ToString("O", System.Globalization.CultureInfo.InvariantCulture) ?? "—"};CheckOut={x.CheckOutTime?.ToString("O", System.Globalization.CultureInfo.InvariantCulture) ?? "—"}";
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private async Task<int> GetIntegerSettingAsync(string key, int defaultValue, CancellationToken cancellationToken)
    {
        var setting = await unitOfWork.Repository<AppSetting>().FirstOrDefaultAsync(x => x.Key == key,
            cancellationToken: cancellationToken);
        return setting is not null && int.TryParse(setting.Value, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var value) && value > 0 ? value : defaultValue;
    }
    private static ChildAttendanceDto ToDto(ChildAttendance x) => new(x.Id, x.ChildId, x.AttendanceDate, x.Status, x.CheckInTime, x.CheckOutTime, x.EntryMethod, x.Notes);
    private static bool IsDuplicate(DbUpdateException ex) => ex.GetBaseException().Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) && ex.GetBaseException().Message.Contains("ChildAttendances", StringComparison.OrdinalIgnoreCase);
}
