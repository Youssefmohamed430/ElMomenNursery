using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Application.Mappings;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;
using System.Linq.Expressions;

namespace ElMomenNursery.Application.Services;

public class ChildService(IUnitOfWork unitOfWork) : IChildService
{
    public async Task<ChildDto> CreateChildAsync(CreateChildDTO childDto)
    {
        var child = childDto.Adapt<Child>();

        await unitOfWork.Repository<Child>().AddAsync(child);
        await unitOfWork.SaveChangesAsync();

        return child.Adapt<ChildDto>();
    }
    public async Task<ChildDto> UpdateChildAsync(Guid childId, UpdateChildDTO childDto)
    {
        var child = await unitOfWork.Repository<Child>().FirstOrDefaultAsync(c => c.Id == childId);

        if (child == null) throw new KeyNotFoundException($"Child with ID '{childId}' was not found.");

        childDto.Adapt(child, MapsterConfigForChild.Config);
        child.UpdatedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync();

        return child.Adapt<ChildDto>();
    }
    public async Task<bool> DeleteChildAsync(Guid childId)
    {
        var child = await unitOfWork.Repository<Child>().FirstOrDefaultAsync(c => c.Id == childId);
        if (child == null) return false;

        unitOfWork.Repository<Child>().Remove(child);
        await unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<List<ChildDto>> GetAllChildrenAsync()
    {
        var childrens = await unitOfWork.Repository<Child>().ListAsync();
        return childrens.Adapt<List<ChildDto>>();
    }

    public async Task<List<ChildDto>> GetChildrenAsync(ChildFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if (filter.DateOfBirthFrom > filter.DateOfBirthTo && filter.DateOfBirthTo.HasValue)
            throw new ArgumentException("Date of birth start must be before or equal to its end.", nameof(filter));
        if (filter.EnrollmentDateFrom > filter.EnrollmentDateTo && filter.EnrollmentDateTo.HasValue)
            throw new ArgumentException("Enrollment start must be before or equal to its end.", nameof(filter));

        Expression<Func<Child, bool>> predicate = child => true;
        var searchText = filter.SearchText?.Trim();
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (!string.IsNullOrEmpty(searchText))
            predicate = And(predicate, child => child.FullName.Contains(searchText));
        if (filter.Gender.HasValue)
            predicate = And(predicate, child => child.Gender == filter.Gender.Value);
        if (filter.DateOfBirthFrom.HasValue)
            predicate = And(predicate, child => child.DateOfBirth >= filter.DateOfBirthFrom.Value);
        if (filter.DateOfBirthTo.HasValue)
            predicate = And(predicate, child => child.DateOfBirth <= filter.DateOfBirthTo.Value);
        if (filter.EnrollmentDateFrom.HasValue)
            predicate = And(predicate, child => child.EnrollmentDate >= filter.EnrollmentDateFrom.Value);
        if (filter.EnrollmentDateTo.HasValue)
            predicate = And(predicate, child => child.EnrollmentDate <= filter.EnrollmentDateTo.Value);
        if (filter.ClassroomId.HasValue)
        {
            predicate = And(predicate, child => child.ClassEnrollments.Any(enrollment =>
                enrollment.ClassroomId == filter.ClassroomId.Value &&
                enrollment.StartDate <= today &&
                (enrollment.EndDate == null || enrollment.EndDate >= today)));
        }
        if (filter.HasPaidSubscription.HasValue)
        {
            if (filter.HasPaidSubscription.Value)
                predicate = And(predicate, child => child.Subscriptions.Any(subscription =>
                    subscription.Status == SubscriptionStatus.Paid &&
                    subscription.PeriodStart <= today &&
                    subscription.PeriodEnd >= today));
            else
                predicate = And(predicate, child => !child.Subscriptions.Any(subscription =>
                    subscription.Status == SubscriptionStatus.Paid &&
                    subscription.PeriodStart <= today &&
                    subscription.PeriodEnd >= today));
        }

        var children = await unitOfWork.Repository<Child>().ListAsync(
            predicate,
            cancellationToken: cancellationToken);
        return children.Adapt<List<ChildDto>>();
    }

    public async Task<List<ClassroomFilterOptionDto>> GetClassroomsForFilterAsync(CancellationToken cancellationToken = default)
    {
        var classrooms = await unitOfWork.Repository<Classroom>().ListAsync(
            classroom => classroom.IsActive,
            cancellationToken: cancellationToken);
        return classrooms
            .OrderBy(classroom => classroom.Name)
            .Select(classroom => new ClassroomFilterOptionDto(classroom.Id, classroom.Name))
            .ToList();
    }

    private static Expression<Func<Child, bool>> And(
        Expression<Func<Child, bool>> left,
        Expression<Func<Child, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(Child), "child");
        var leftBody = new ReplaceParameterVisitor(left.Parameters[0], parameter).Visit(left.Body)!;
        var rightBody = new ReplaceParameterVisitor(right.Parameters[0], parameter).Visit(right.Body)!;
        return Expression.Lambda<Func<Child, bool>>(Expression.AndAlso(leftBody, rightBody), parameter);
    }

    private sealed class ReplaceParameterVisitor(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == source ? target : base.VisitParameter(node);
    }

    public async Task<ChildDto> GetChildByIdAsync(Guid childId)
    {
        var child = await unitOfWork.Repository<Child>().FirstOrDefaultAsync(c => c.Id == childId);
        return child.Adapt<ChildDto>();
    }

    public async Task<ChildDto> GetChildByNameAsync(string childName)
    {
        var child = await unitOfWork.Repository<Child>().FirstOrDefaultAsync(c => c.FullName == childName);
        return child.Adapt<ChildDto>();
    }

    public async Task<List<ChildDto>> GetChildrenByDateOfBirthAsync(DateOnly dateOfBirth)
    {
        var children = await unitOfWork.Repository<Child>().ListAsync(c => c.DateOfBirth == dateOfBirth);
        return children.Adapt<List<ChildDto>>();
    }

    public async Task<List<ChildDto>> GetChildrenByGenderAsync(ChildGender gender)
    {
        var children = await unitOfWork.Repository<Child>().ListAsync(c => c.Gender == gender);
        return children.Adapt<List<ChildDto>>();
    }

    public async Task<List<ChildDto>> GetChildrenByRangeOfEnrollmentDateAsync(DateOnly startDate, DateOnly endDate)
    {
        var children = await unitOfWork.Repository<Child>().ListAsync(c => c.EnrollmentDate >= startDate && c.EnrollmentDate <= endDate);
        return children.Adapt<List<ChildDto>>();
    }

    public Task<List<ChildDto>> GetChildrenWithoutPaidSubscriptions(int parentId) =>
        GetChildrenAsync(new ChildFilter { HasPaidSubscription = false });

    public Task<List<ChildDto>> GetChildrenWithPaidSubscriptions(int parentId) =>
        GetChildrenAsync(new ChildFilter { HasPaidSubscription = true });
}
