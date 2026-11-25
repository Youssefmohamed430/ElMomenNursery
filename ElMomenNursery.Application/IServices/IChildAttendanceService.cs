using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Application.IServices;

public interface IChildAttendanceService
{
    Task<Result<IReadOnlyList<ChildDailyAttendanceDto>>> GetDailyAttendanceAsync(ChildDailyAttendanceQuery query, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<AttendanceChildOptionDto>>> GetChildrenForHistorySelectionAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<AttendanceChildOptionDto>>> GetChildrenForAttendanceSheetAsync(Guid classroomId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ClassroomFilterOptionDto>>> GetClassroomsForFilterAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ChildAttendanceDto>>> GetChildAttendanceByRangeAsync(Guid childId, DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
    Task<Result<ChildAttendanceDto>> MarkManualAttendanceAsync(CreateChildAttendanceDto dto, CancellationToken cancellationToken = default);
    Task<Result<ChildAttendanceDto>> UpdateChildAttendanceAsync(Guid id, UpdateChildAttendanceDto dto, CancellationToken cancellationToken = default);
    Task<Result<ChildAttendanceDto>> RecordCheckOutAsync(Guid childId, DateOnly date, DateTimeOffset checkOut, CancellationToken cancellationToken = default);
    Task<Result<int>> MarkAllPresentAsync(DateOnly date, Guid? classroomId, CancellationToken cancellationToken = default);
    Task<Result<int>> MarkBulkAsync(DateOnly date, IReadOnlyCollection<Guid> childIds, AttendanceStatus status, DateTimeOffset? checkIn, CancellationToken cancellationToken = default);
}
