using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Application.Mappings;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;
using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ElMomenNursery.Application.Services;

public class ChildService(IUnitOfWork unitOfWork, ILogger<ChildService> logger, IPermissionService permissions) : IChildService
{
    public async Task<ChildDto> CreateChildAsync(CreateChildDTO childDto)
    {
        permissions.EnsurePermission("Students.Create");
        ArgumentNullException.ThrowIfNull(childDto);
        try
        {
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(childDto, new ValidationContext(childDto), validationResults, validateAllProperties: true))
            {
                logger.LogWarning("Child creation rejected by validation; errorCount={ErrorCount}", validationResults.Count);
                throw new ValidationException("بيانات الطفل غير مكتملة أو غير صحيحة.");
            }
            var child = childDto.Adapt<Child>();
            await unitOfWork.Repository<Child>().AddAsync(child);
            var affectedRows = await unitOfWork.SaveChangesAsync();
            if (affectedRows == 0)
                logger.LogWarning("Child creation saved no rows; childId={ChildId}", child.Id);
            logger.LogInformation("Created child record; childId={ChildId}", child.Id);
            return child.Adapt<ChildDto>();
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Child creation rejected by database constraint; constraintKind={ConstraintKind}", GetConstraintKind(exception));
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "ChildService failed to create child record");
            throw;
        }
    }
    public async Task<ChildDto> UpdateChildAsync(Guid childId, UpdateChildDTO childDto)
    {
        permissions.EnsurePermission("Students.Edit");
        try
        {
            var child = await unitOfWork.Repository<Child>().FirstOrDefaultAsync(c => c.Id == childId, asNoTracking: false);
            if (child == null) throw new KeyNotFoundException($"Child with ID '{childId}' was not found.");
            childDto.Adapt(child, MapsterConfigForChild.Config);
            child.UpdatedAt = DateTimeOffset.UtcNow;
            var affectedRows = await unitOfWork.SaveChangesAsync();
            if (affectedRows == 0)
                logger.LogWarning("Child update saved no rows; childId={ChildId}", childId);
            logger.LogInformation("Updated child record; childId={ChildId}", childId);
            return child.Adapt<ChildDto>();
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Child update rejected by database constraint; childId={ChildId}; constraintKind={ConstraintKind}", childId, GetConstraintKind(exception));
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "ChildService failed to update child record; childId={ChildId}", childId);
            throw;
        }
    }
    public async Task<bool> DeleteChildAsync(Guid childId)
    {
        permissions.EnsurePermission("Students.Delete");
        try
        {
            var child = await unitOfWork.Repository<Child>().FirstOrDefaultAsync(c => c.Id == childId);
            if (child == null) return false;
            unitOfWork.Repository<Child>().Remove(child);
            var affectedRows = await unitOfWork.SaveChangesAsync();
            if (affectedRows == 0)
                logger.LogWarning("Child deletion saved no rows; childId={ChildId}", childId);
            logger.LogInformation("Deleted child record; childId={ChildId}", childId);
            return true;
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Child deletion rejected by database constraint; childId={ChildId}; constraintKind={ConstraintKind}", childId, GetConstraintKind(exception));
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "ChildService failed to delete child record; childId={ChildId}", childId);
            throw;
        }
    }

    public async Task<List<ChildDto>> GetAllChildrenAsync()
    {
        permissions.EnsurePermission("Students.View");
        var childrens = await unitOfWork.Repository<Child>().ListAsync();
        return childrens.Adapt<List<ChildDto>>();
    }

    public async Task<List<ChildDto>> GetChildrenAsync(ChildFilter filter, CancellationToken cancellationToken = default)
    {
        permissions.EnsurePermission("Students.View");
        var activeFilterCount = CountActiveFilters(filter);
        logger.LogDebug("Loading children; activeFilterCount={ActiveFilterCount}", activeFilterCount);
        try
        {
            ArgumentNullException.ThrowIfNull(filter);

            if (filter.DateOfBirthFrom > filter.DateOfBirthTo && filter.DateOfBirthTo.HasValue)
            {
                logger.LogWarning("Child search rejected because date of birth range is invalid");
                throw new ArgumentException("Date of birth start must be before or equal to its end.", nameof(filter));
            }
            if (filter.EnrollmentDateFrom > filter.EnrollmentDateTo && filter.EnrollmentDateTo.HasValue)
            {
                logger.LogWarning("Child search rejected because enrollment date range is invalid");
                throw new ArgumentException("Enrollment start must be before or equal to its end.", nameof(filter));
            }

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
        logger.LogDebug("Loaded children; resultCount={ResultCount}", children.Count);
        return children.Adapt<List<ChildDto>>();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "ChildService failed to load child records");
            throw;
        }
    }

    public async Task<List<ClassroomFilterOptionDto>> GetClassroomsForFilterAsync(CancellationToken cancellationToken = default)
    {
        permissions.EnsurePermission("Students.View");
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
        permissions.EnsurePermission("Students.View");
        var child = await unitOfWork.Repository<Child>().FirstOrDefaultAsync(c => c.Id == childId);
        if (child is null)
        {
            logger.LogWarning("Child record was not found; childId={ChildId}", childId);
            throw new KeyNotFoundException($"Child '{childId}' was not found.");
        }
        return child.Adapt<ChildDto>();
    }

    public async Task<ChildDto> GetChildByNameAsync(string childName)
    {
        permissions.EnsurePermission("Students.View");
        var child = await unitOfWork.Repository<Child>().FirstOrDefaultAsync(c => c.FullName == childName);
        if (child is null)
        {
            logger.LogWarning("Child record was not found by name lookup");
            throw new KeyNotFoundException("Child record was not found.");
        }
        return child.Adapt<ChildDto>();
    }

    public async Task<List<ChildDto>> GetChildrenByDateOfBirthAsync(DateOnly dateOfBirth)
    {
        permissions.EnsurePermission("Students.View");
        var children = await unitOfWork.Repository<Child>().ListAsync(c => c.DateOfBirth == dateOfBirth);
        return children.Adapt<List<ChildDto>>();
    }

    public async Task<List<ChildDto>> GetChildrenByGenderAsync(ChildGender gender)
    {
        permissions.EnsurePermission("Students.View");
        var children = await unitOfWork.Repository<Child>().ListAsync(c => c.Gender == gender);
        return children.Adapt<List<ChildDto>>();
    }

    public async Task<List<ChildDto>> GetChildrenByRangeOfEnrollmentDateAsync(DateOnly startDate, DateOnly endDate)
    {
        permissions.EnsurePermission("Students.View");
        var children = await unitOfWork.Repository<Child>().ListAsync(c => c.EnrollmentDate >= startDate && c.EnrollmentDate <= endDate);
        return children.Adapt<List<ChildDto>>();
    }

    public Task<List<ChildDto>> GetChildrenWithoutPaidSubscriptions(int parentId) =>
        GetChildrenAsync(new ChildFilter { HasPaidSubscription = false });

    public Task<List<ChildDto>> GetChildrenWithPaidSubscriptions(int parentId) =>
        GetChildrenAsync(new ChildFilter { HasPaidSubscription = true });

    private static string GetConstraintKind(DbUpdateException exception)
    {
        var details = exception.GetBaseException().ToString();
        if (details.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase)) return "UNIQUE";
        if (details.Contains("FOREIGN KEY constraint", StringComparison.OrdinalIgnoreCase)) return "FOREIGN KEY";
        if (details.Contains("CHECK constraint", StringComparison.OrdinalIgnoreCase)) return "CHECK";
        return "UNKNOWN";
    }

    private static int CountActiveFilters(ChildFilter? filter)
    {
        if (filter is null) return 0;
        return new[]
        {
            !string.IsNullOrWhiteSpace(filter.SearchText), filter.Gender.HasValue,
            filter.DateOfBirthFrom.HasValue, filter.DateOfBirthTo.HasValue,
            filter.EnrollmentDateFrom.HasValue, filter.EnrollmentDateTo.HasValue,
            filter.ClassroomId.HasValue, filter.HasPaidSubscription.HasValue
        }.Count(active => active);
    }
}
