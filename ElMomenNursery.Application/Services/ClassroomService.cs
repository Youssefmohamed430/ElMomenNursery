namespace ElMomenNursery.Application.Services;

public sealed class ClassroomService(IUnitOfWork unitOfWork, IPermissionService permissions,
    ISessionManager sessions, ILogger<ClassroomService> logger) : IClassroomService
{
    public async Task<Result<IReadOnlyList<ClassroomDto>>> GetClassroomsAsync(CancellationToken cancellationToken = default)
    {
        if (!Allowed()) return ClassroomErrors.PermissionDenied;
        var rooms = await unitOfWork.Repository<Classroom>().Query()
            .Include(x => x.TeacherAssignments).ThenInclude(x => x.Employee)
            .Include(x => x.ChildEnrollments).ToListAsync(cancellationToken);
        var result = rooms.OrderBy(x => x.Name).Select(ToDto).ToArray();
        return Result.Success<IReadOnlyList<ClassroomDto>>(result);
    }

    public async Task<Result<IReadOnlyList<ClassroomTeacherDto>>> GetTeachersAsync(CancellationToken cancellationToken = default)
    {
        if (!Allowed()) return ClassroomErrors.PermissionDenied;
        var employees = await unitOfWork.Repository<Employee>().ListAsync(cancellationToken: cancellationToken);
        return Result.Success<IReadOnlyList<ClassroomTeacherDto>>(employees.OrderBy(x => x.FullName)
            .Select(x => new ClassroomTeacherDto(x.Id, x.FullName)).ToArray());
    }

    public async Task<Result<ClassroomDto>> SaveClassroomAsync(Guid? classroomId, string name, Guid teacherId, CancellationToken cancellationToken = default)
    {
        if (!Allowed()) return ClassroomErrors.PermissionDenied;
        name = name?.Trim() ?? string.Empty;
        if (name.Length is 0 or > 100 || teacherId == Guid.Empty) return ClassroomErrors.InvalidData;
        try
        {
            var employee = await unitOfWork.Repository<Employee>().FirstOrDefaultAsync(x => x.Id == teacherId,
                asNoTracking: false, cancellationToken: cancellationToken);
            if (employee is null) return ClassroomErrors.InvalidData;
            var rooms = unitOfWork.Repository<Classroom>();
            var room = classroomId.HasValue
                ? await rooms.Query(asNoTracking: false).Include(x => x.TeacherAssignments)
                    .ThenInclude(x => x.Employee).Include(x => x.ChildEnrollments)
                    .SingleOrDefaultAsync(x => x.Id == classroomId.Value, cancellationToken)
                : null;
            if (classroomId.HasValue && room is null) return ClassroomErrors.NotFound;
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (room is null)
            {
                room = new Classroom { Name = name };
                await rooms.AddAsync(room, cancellationToken);
            }
            else room.Name = name;
            var active = room.TeacherAssignments.FirstOrDefault(x => x.EndDate is null);
            if (active?.EmployeeId != employee.Id)
            {
                if (active is not null) active.EndDate = today < active.StartDate ? active.StartDate : today;
                await unitOfWork.Repository<ClassTeacherAssignment>().AddAsync(new ClassTeacherAssignment
                {
                    Classroom = room, ClassroomId = room.Id, Employee = employee, EmployeeId = employee.Id, StartDate = today
                }, cancellationToken);
            }
            await AuditAsync(classroomId.HasValue ? "Classroom.Updated" : "Classroom.Created", room.Id, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Classroom saved; operation={Operation}; classroomId={ClassroomId}; teacherId={TeacherId}",
                classroomId.HasValue ? "Update" : "Create", room.Id, employee.Id);
            return Result.Success(ToDto(room), classroomId.HasValue ? "تم تحديث الفصل." : "تم إنشاء الفصل.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Classroom save failed; classroomId={ClassroomId}; teacherId={TeacherId}", classroomId, teacherId);
            return ex is DbUpdateException ? PersistenceErrorMapper.FromException(ex) : ClassroomErrors.PersistenceFailure;
        }
    }

    public async Task<Result> DeleteClassroomAsync(Guid classroomId, CancellationToken cancellationToken = default)
    {
        if (!Allowed()) return ClassroomErrors.PermissionDenied;
        try
        {
            var room = await unitOfWork.Repository<Classroom>().Query(asNoTracking: false)
                .Include(x => x.TeacherAssignments).Include(x => x.ChildEnrollments)
                .SingleOrDefaultAsync(x => x.Id == classroomId, cancellationToken);
            if (room is null) return ClassroomErrors.NotFound;
            var today = DateOnly.FromDateTime(DateTime.Today);
            foreach (var row in room.ChildEnrollments.Where(x => x.EndDate is null)) row.EndDate = today < row.StartDate ? row.StartDate : today;
            foreach (var row in room.TeacherAssignments.Where(x => x.EndDate is null)) row.EndDate = today < row.StartDate ? row.StartDate : today;
            unitOfWork.Repository<Classroom>().Remove(room);
            await AuditAsync("Classroom.Deleted", room.Id, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Classroom deleted; classroomId={ClassroomId}", classroomId);
            return Result.Success("تم نقل الفصل إلى المحذوفات وإنهاء إسناداته الحالية.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Classroom deletion failed; classroomId={ClassroomId}", classroomId);
            return ex is DbUpdateException ? PersistenceErrorMapper.FromException(ex) : ClassroomErrors.PersistenceFailure;
        }
    }

    public async Task<Result<IReadOnlyList<ClassroomChildDto>>> GetChildrenAsync(Guid classroomId, CancellationToken cancellationToken = default)
    {
        if (!Allowed()) return ClassroomErrors.PermissionDenied;
        if (!await unitOfWork.Repository<Classroom>().AnyAsync(x => x.Id == classroomId, cancellationToken)) return ClassroomErrors.NotFound;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var assigned = (await unitOfWork.Repository<ChildClassEnrollment>().ListAsync(x => x.ClassroomId == classroomId
            && x.StartDate <= today && (x.EndDate == null || x.EndDate >= today), cancellationToken: cancellationToken))
            .Select(x => x.ChildId).ToHashSet();
        var children = await unitOfWork.Repository<Child>().ListAsync(cancellationToken: cancellationToken);
        return Result.Success<IReadOnlyList<ClassroomChildDto>>(children.OrderBy(x => x.FullName)
            .Select(x => new ClassroomChildDto(x.Id, x.FullName, assigned.Contains(x.Id))).ToArray());
    }

    public async Task<Result> AssignChildrenAsync(Guid classroomId, IReadOnlyCollection<Guid> childIds, CancellationToken cancellationToken = default)
    {
        if (!Allowed()) return ClassroomErrors.PermissionDenied;
        try
        {
            var room = await unitOfWork.Repository<Classroom>().FirstOrDefaultAsync(x => x.Id == classroomId,
                asNoTracking: false, cancellationToken: cancellationToken);
            if (room is null) return ClassroomErrors.NotFound;
            var today = DateOnly.FromDateTime(DateTime.Today);
            var selected = childIds.ToHashSet();
            var children = await unitOfWork.Repository<Child>().ListAsync(x => selected.Contains(x.Id), cancellationToken: cancellationToken);
            if (children.Count != selected.Count) return ChildErrors.NotFound;
            var activeRows = await unitOfWork.Repository<ChildClassEnrollment>().Query(asNoTracking: false)
                .Where(x => x.EndDate == null && x.StartDate <= today).ToListAsync(cancellationToken);
            foreach (var row in activeRows.Where(x => x.ClassroomId == classroomId && !selected.Contains(x.ChildId)))
                row.EndDate = today < row.StartDate ? row.StartDate : today;
            foreach (var child in children)
            {
                if (activeRows.Any(x => x.ChildId == child.Id && x.ClassroomId == classroomId)) continue;
                foreach (var previous in activeRows.Where(x => x.ChildId == child.Id && x.ClassroomId != classroomId))
                    previous.EndDate = today < previous.StartDate ? previous.StartDate : today;
                var sameDay = await unitOfWork.Repository<ChildClassEnrollment>().FirstOrDefaultAsync(
                    x => x.ChildId == child.Id && x.ClassroomId == classroomId && x.StartDate == today,
                    asNoTracking: false, cancellationToken: cancellationToken);
                if (sameDay is not null) sameDay.EndDate = null;
                else await unitOfWork.Repository<ChildClassEnrollment>().AddAsync(new ChildClassEnrollment
                {
                    Child = child, ChildId = child.Id, Classroom = room, ClassroomId = classroomId, StartDate = today
                }, cancellationToken);
            }
            await AuditAsync("Classroom.ChildrenAssigned", classroomId, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Classroom children assigned; classroomId={ClassroomId}; childCount={ChildCount}", classroomId, selected.Count);
            return Result.Success("تم تحديث إسناد الأطفال.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Classroom children assignment failed; classroomId={ClassroomId}", classroomId);
            return ex is DbUpdateException ? PersistenceErrorMapper.FromException(ex) : ClassroomErrors.PersistenceFailure;
        }
    }

    public async Task<Result<Guid?>> GetChildClassroomAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        if (!Allowed()) return ClassroomErrors.PermissionDenied;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var enrollment = await unitOfWork.Repository<ChildClassEnrollment>().Query()
            .Where(x => x.ChildId == childId && x.StartDate <= today && (x.EndDate == null || x.EndDate >= today))
            .OrderByDescending(x => x.StartDate).FirstOrDefaultAsync(cancellationToken);
        return Result.Success<Guid?>(enrollment?.ClassroomId);
    }

    public async Task<Result<IReadOnlyList<Guid>>> GetChildClassroomsAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        if (!Allowed()) return ClassroomErrors.PermissionDenied;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var ids = await unitOfWork.Repository<ChildClassEnrollment>().Query()
            .Where(x => x.ChildId == childId && x.StartDate <= today && (x.EndDate == null || x.EndDate >= today))
            .OrderBy(x => x.StartDate).Select(x => x.ClassroomId).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<Guid>>(ids);
    }

    public async Task<Result> SetChildClassroomsAsync(Guid childId, IReadOnlyCollection<Guid> classroomIds, DateOnly startDate, CancellationToken cancellationToken = default)
    {
        if (!Allowed()) return ClassroomErrors.PermissionDenied;
        var selected = classroomIds.Where(x => x != Guid.Empty).Distinct().ToArray();
        if (selected.Length > 2) return new Error("Classes.TooManyForChild", "يمكن إسناد الطفل إلى فصلين كحد أقصى.");
        if (startDate == default) return ClassroomErrors.InvalidData;
        try
        {
            var child = await unitOfWork.Repository<Child>().FirstOrDefaultAsync(x => x.Id == childId, asNoTracking: false, cancellationToken: cancellationToken);
            if (child is null) return ChildErrors.NotFound;
            var rooms = await unitOfWork.Repository<Classroom>().ListAsync(x => selected.Contains(x.Id), asNoTracking: false, cancellationToken: cancellationToken);
            if (rooms.Count != selected.Length) return ClassroomErrors.NotFound;
            var today = DateOnly.FromDateTime(DateTime.Today);
            var enrollments = unitOfWork.Repository<ChildClassEnrollment>();
            var current = await enrollments.Query(asNoTracking: false).Where(x => x.ChildId == childId && x.StartDate <= today && (x.EndDate == null || x.EndDate >= today)).ToListAsync(cancellationToken);
            foreach (var row in current.Where(x => !selected.Contains(x.ClassroomId))) row.EndDate = today < row.StartDate ? row.StartDate : today;
            foreach (var room in rooms.Where(r => current.All(x => x.ClassroomId != r.Id)))
            {
                var sameDay = await enrollments.FirstOrDefaultAsync(x => x.ChildId == childId && x.ClassroomId == room.Id && x.StartDate == startDate, asNoTracking: false, cancellationToken: cancellationToken);
                if (sameDay is not null) sameDay.EndDate = null;
                else await enrollments.AddAsync(new ChildClassEnrollment { ChildId = child.Id, Child = child, ClassroomId = room.Id, Classroom = room, StartDate = startDate }, cancellationToken);
            }
            await AuditAsync("Classroom.ChildAssignmentUpdated", childId, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success("تم تحديث فصول الطفل.");
        }
        catch (Exception ex) { logger.LogError(ex, "Child classroom assignments update failed; childId={ChildId}", childId); return ex is DbUpdateException ? PersistenceErrorMapper.FromException(ex) : ClassroomErrors.PersistenceFailure; }
    }

    public async Task<Result> AssignChildAsync(Guid childId, Guid? classroomId, DateOnly startDate, CancellationToken cancellationToken = default)
    {
        if (!Allowed()) return ClassroomErrors.PermissionDenied;
        if (startDate == default) return ClassroomErrors.InvalidData;
        try
        {
            var child = await unitOfWork.Repository<Child>().FirstOrDefaultAsync(x => x.Id == childId,
                asNoTracking: false, cancellationToken: cancellationToken);
            if (child is null) return ChildErrors.NotFound;
            var today = DateOnly.FromDateTime(DateTime.Today);
            var endDate = DateOnly.FromDateTime(DateTime.Today);
            var current = await unitOfWork.Repository<ChildClassEnrollment>().Query(asNoTracking: false)
                .Where(x => x.ChildId == childId && x.StartDate <= today && (x.EndDate == null || x.EndDate >= today))
                .ToListAsync(cancellationToken);
            if (current.Any(x => x.ClassroomId == classroomId)) return Result.Success();
            foreach (var row in current) row.EndDate = endDate < row.StartDate ? row.StartDate : endDate;
            if (classroomId.HasValue)
            {
                var room = await unitOfWork.Repository<Classroom>().FirstOrDefaultAsync(x => x.Id == classroomId.Value,
                    asNoTracking: false, cancellationToken: cancellationToken);
                if (room is null) return ClassroomErrors.NotFound;
                var effectiveStartDate = startDate;
                var repo = unitOfWork.Repository<ChildClassEnrollment>();
                var sameDay = await repo.FirstOrDefaultAsync(x => x.ChildId == childId && x.ClassroomId == room.Id && x.StartDate == effectiveStartDate,
                    asNoTracking: false, cancellationToken: cancellationToken);
                if (sameDay is not null) sameDay.EndDate = null;
                else await repo.AddAsync(new ChildClassEnrollment { ChildId = childId, ClassroomId = room.Id, StartDate = effectiveStartDate, Child = child, Classroom = room }, cancellationToken);
            }
            await AuditAsync("Classroom.ChildAssignmentUpdated", childId, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Child classroom assignment updated; childId={ChildId}; classroomId={ClassroomId}", childId, classroomId);
            return Result.Success("تم تحديث فصل الطفل.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Child classroom assignment failed; childId={ChildId}; classroomId={ClassroomId}", childId, classroomId);
            return ex is DbUpdateException ? PersistenceErrorMapper.FromException(ex) : ClassroomErrors.PersistenceFailure;
        }
    }

    private bool Allowed() => permissions.HasPermission("Classes.Manage");
    private async Task AuditAsync(string action, Guid id, CancellationToken ct) =>
        await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
        {
            Action = action, EntityType = nameof(Classroom), EntityId = id.ToString("D"),
            UserAccountId = sessions.CurrentUser?.UserId, ChangeSummary = action
        }, ct);
    private static ClassroomDto ToDto(Classroom room)
    {
        var assignment = room.TeacherAssignments.Where(x => x.EndDate is null)
            .OrderByDescending(x => x.StartDate).FirstOrDefault();
        var today = DateOnly.FromDateTime(DateTime.Today);
        return new ClassroomDto(room.Id, room.Name, assignment?.EmployeeId, assignment?.Employee.FullName,
            room.ChildEnrollments.Count(x => x.StartDate <= today && (x.EndDate == null || x.EndDate >= today)));
    }
}
