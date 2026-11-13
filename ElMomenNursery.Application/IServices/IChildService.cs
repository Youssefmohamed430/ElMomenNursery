using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.IServices;

public interface IChildService
{
    Task<List<ChildDto>> GetAllChildrenAsync();
    Task<List<ChildDto>> GetChildrenAsync(ChildFilter filter, CancellationToken cancellationToken = default);
    Task<List<ClassroomFilterOptionDto>> GetClassroomsForFilterAsync(CancellationToken cancellationToken = default);
    Task<List<ChildDto>> GetChildrenWithPaidSubscriptions(int parentId);
    Task<List<ChildDto>> GetChildrenWithoutPaidSubscriptions(int parentId);
    Task<List<ChildDto>> GetChildrenByDateOfBirthAsync(DateOnly dateOfBirth);
    Task<List<ChildDto>> GetChildrenByRangeOfEnrollmentDateAsync(DateOnly startDate, DateOnly endDate);
    Task<List<ChildDto>> GetChildrenByGenderAsync(ChildGender gender);
    Task<ChildDto> GetChildByIdAsync(Guid childId);
    Task<ChildDto> GetChildByNameAsync(string childName);
    Task<ChildDto> CreateChildAsync(CreateChildDTO childDto);
    Task<ChildDto> UpdateChildAsync(Guid childId, UpdateChildDTO childDto);
    Task<bool> DeleteChildAsync(Guid childId);
}
