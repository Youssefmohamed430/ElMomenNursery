using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.IServices;

public interface IChildService
{
    Task<Result<IReadOnlyList<ChildDto>>> GetAllChildrenAsync();
    Task<Result<IReadOnlyList<ChildDto>>> GetDeletedChildrenAsync(CancellationToken cancellationToken = default);
    Task<Result> RestoreChildAsync(Guid childId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ChildDto>>> GetChildrenAsync(ChildFilter filter, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ClassroomFilterOptionDto>>> GetClassroomsForFilterAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ChildDto>>> GetChildrenByDateOfBirthAsync(DateOnly dateOfBirth);
    Task<Result<IReadOnlyList<ChildDto>>> GetChildrenByRangeOfEnrollmentDateAsync(DateOnly startDate, DateOnly endDate);
    Task<Result<IReadOnlyList<ChildDto>>> GetChildrenByGenderAsync(ChildGender gender);
    Task<Result<ChildDto>> GetChildByIdAsync(Guid childId);
    Task<Result<ChildDto>> GetChildByNameAsync(string childName);
    //Task<Result<List<ChildDto>>> GetChildsNearingForSchoolAgeAsync();
    Task<Result<IReadOnlyList<ChildAttachmentDto>>> GetChildAttachmentsAsync(Guid childId, CancellationToken cancellationToken = default);
    Task<Result> AddChildAttachmentAsync(Guid childId, ChildAttachmentDto attachment, CancellationToken cancellationToken = default);
    Task<Result<ChildDto>> CreateChildAsync(CreateChildDTO childDto);
    Task<Result<ChildDto>> CreatePendingRegistrationAsync(string childName, CancellationToken cancellationToken = default);
    Task<Result<ChildDto>> UpdateChildAsync(Guid childId, UpdateChildDTO childDto);
    Task<Result> DeleteChildAsync(Guid childId);
}
