using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Abstractions;

namespace ElMomenNursery.Application.IServices;

public interface IClassroomService
{
    Task<Result<IReadOnlyList<ClassroomDto>>> GetClassroomsAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ClassroomTeacherDto>>> GetTeachersAsync(CancellationToken cancellationToken = default);
    Task<Result<ClassroomDto>> SaveClassroomAsync(Guid? classroomId, string name, Guid teacherId, CancellationToken cancellationToken = default);
    Task<Result> DeleteClassroomAsync(Guid classroomId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ClassroomChildDto>>> GetChildrenAsync(Guid classroomId, CancellationToken cancellationToken = default);
    Task<Result> AssignChildrenAsync(Guid classroomId, IReadOnlyCollection<Guid> childIds, CancellationToken cancellationToken = default);
    Task<Result<Guid?>> GetChildClassroomAsync(Guid childId, CancellationToken cancellationToken = default);
    Task<Result> AssignChildAsync(Guid childId, Guid? classroomId, DateOnly startDate, CancellationToken cancellationToken = default);
}
