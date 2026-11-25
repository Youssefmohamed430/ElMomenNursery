using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Application.Common;
using ElMomenNursery.Application.DTOs;
namespace ElMomenNursery.Application.Services;

public class ChildService(IUnitOfWork unitOfWork, ILogger<ChildService> logger, IPermissionService permissions) : IChildService
{
    public async Task<Result<ChildDto>> CreateChildAsync(CreateChildDTO childDto)
    {
        var permissionError = PermissionGuard.Check(permissions, "Students.Create", ChildErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        if (childDto is null) return ChildErrors.InvalidData;
        if (!ValidChildFields(childDto)) return ChildErrors.InvalidData;
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(childDto, new ValidationContext(childDto), validationResults, validateAllProperties: true))
        {
            logger.LogWarning("Child creation rejected by validation; errorCount={ErrorCount}", validationResults.Count);
            var details = string.Join("\n", validationResults.Select(x => x.ErrorMessage).Where(x => !string.IsNullOrWhiteSpace(x)));
            return new Error(ChildErrors.InvalidData.Code, string.IsNullOrWhiteSpace(details) ? ChildErrors.InvalidData.Description : details);
        }
        if (childDto.DateOfBirth > DateOnly.FromDateTime(DateTime.Today) || childDto.DateOfBirth > childDto.EnrollmentDate)
            return ChildErrors.InvalidData;
        try
        {
            var child = childDto.Adapt<Child>();
            await unitOfWork.Repository<Child>().AddAsync(child);
            var affectedRows = await unitOfWork.SaveChangesAsync();

            if (affectedRows == 0)
                logger.LogWarning("Child creation saved no rows; childId={ChildId}", child.Id);
            logger.LogInformation("Created child record; childId={ChildId}", child.Id);
            return Result.Success(child.Adapt<ChildDto>(), ChildErrors.Created.Description);
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Child creation rejected by database constraint; errorCode={ErrorCode}", PersistenceErrorMapper.FromException(exception).Code);
            return PersistenceErrorMapper.FromException(exception);
        }
    }
    public async Task<Result<ChildDto>> CreatePendingRegistrationAsync(string childName, CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Students.Create", ChildErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        var name = childName?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100) return ChildErrors.InvalidData;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var child = new Child
        {
            FullName = name, Address = string.Empty, EnrollmentDate = today, DateOfBirth = today,
            Gender = ChildGender.Boy, FatherName = string.Empty, FatherPhone = string.Empty,
            MotherName = string.Empty, MotherPhone = string.Empty, Shift = Shift.Morning,
            IsRegistrationComplete = false
        };
        await unitOfWork.Repository<Child>().AddAsync(child, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(child.Adapt<ChildDto>(), "تم تسجيل اسم الطفل كتسجيل مبدئي.");
    }
    public async Task<Result<ChildDto>> UpdateChildAsync(Guid childId, UpdateChildDTO childDto)
    {
        var permissionError = PermissionGuard.Check(permissions, "Students.Edit", ChildErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        if (childDto is null) return ChildErrors.InvalidData;
        if (!ValidChildFields(childDto)) return ChildErrors.InvalidData;
        if (childDto.DateOfBirth > DateOnly.FromDateTime(DateTime.Today) || childDto.DateOfBirth > childDto.EnrollmentDate)
            return ChildErrors.InvalidData;
        try
        {
            var child = await unitOfWork.Repository<Child>().FirstOrDefaultAsync(c => c.Id == childId, asNoTracking: false);
            if (child == null) return ChildErrors.NotFound;
            childDto.Adapt(child, MapsterConfigForChild.Config);
            child.IsRegistrationComplete = true;
            child.UpdatedAt = DateTimeOffset.UtcNow;
            var affectedRows = await unitOfWork.SaveChangesAsync();
            if (affectedRows == 0)
                logger.LogWarning("Child update saved no rows; childId={ChildId}", childId);
            logger.LogInformation("Updated child record; childId={ChildId}", childId);
            return Result.Success(child.Adapt<ChildDto>(), ChildErrors.Updated.Description);
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Child update rejected by database constraint; childId={ChildId}; errorCode={ErrorCode}", childId, PersistenceErrorMapper.FromException(exception).Code);
            return PersistenceErrorMapper.FromException(exception);
        }
    }
    public async Task<Result> DeleteChildAsync(Guid childId)
    {
        var permissionError = PermissionGuard.Check(permissions, "Students.Delete", ChildErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        try
        {
            var child = await unitOfWork.Repository<Child>().FirstOrDefaultAsync(c => c.Id == childId);
            if (child == null) return ChildErrors.NotFound;

            unitOfWork.Repository<Child>().Remove(child);
            var affectedRows = await unitOfWork.SaveChangesAsync();
            if (affectedRows == 0)
                logger.LogWarning("Child deletion saved no rows; childId={ChildId}", childId);
            logger.LogInformation("Deleted child record; childId={ChildId}", childId);
            return Result.Success(ChildErrors.Deleted.Description);
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Child deletion rejected by database constraint; childId={ChildId}; errorCode={ErrorCode}", childId, PersistenceErrorMapper.FromException(exception).Code);
            return PersistenceErrorMapper.FromException(exception);
        }
    }

    public async Task<Result<IReadOnlyList<ChildDto>>> GetAllChildrenAsync()
    {
        var permissionError = PermissionGuard.Check(permissions, "Students.View", ChildErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        var childrens = await unitOfWork.Repository<Child>().ListAsync();
        return Result.Success<IReadOnlyList<ChildDto>>(childrens.Adapt<List<ChildDto>>());
    }

    public async Task<Result<IReadOnlyList<ChildDto>>> GetDeletedChildrenAsync(CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Students.Restore", ChildErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        var children = await unitOfWork.Repository<Child>().Query().IgnoreQueryFilters().Where(x => x.IsDeleted).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ChildDto>>(children.Adapt<List<ChildDto>>());
    }

    public async Task<Result> RestoreChildAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Students.Restore", ChildErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        var child = await unitOfWork.Repository<Child>().Query(asNoTracking: false).IgnoreQueryFilters()
            .SingleOrDefaultAsync(x => x.Id == childId && x.IsDeleted, cancellationToken);
        if (child is null) return ChildErrors.NotFound;
        child.IsDeleted = false; child.DeletedAt = null; child.DeletedByUserId = null; child.UpdatedAt = DateTimeOffset.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(ChildErrors.Restored.Description);
    }

    public async Task<Result<IReadOnlyList<ChildDto>>> GetChildrenAsync(ChildFilter filter, CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Students.View", ChildErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        if (filter is null) return ChildErrors.InvalidFilter;
        var activeFilterCount = CountActiveFilters(filter);
        logger.LogDebug("Loading children; activeFilterCount={ActiveFilterCount}", activeFilterCount);
            if (filter.DateOfBirthFrom > filter.DateOfBirthTo && filter.DateOfBirthTo.HasValue)
            {
                logger.LogWarning("Child search rejected because date of birth range is invalid");
                return ChildErrors.InvalidFilter;
            }
            if (filter.EnrollmentDateFrom > filter.EnrollmentDateTo && filter.EnrollmentDateTo.HasValue)
            {
                logger.LogWarning("Child search rejected because enrollment date range is invalid");
                return ChildErrors.InvalidFilter;
            }

        Expression<Func<Child, bool>> predicate = child => true;
        var searchText = ArabicNameNormalizer.Normalize(filter.SearchText);
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (filter.Gender.HasValue)
            predicate = And(predicate, child => child.Gender == filter.Gender.Value);
        if (filter.Shift.HasValue)
            predicate = And(predicate, child => child.Shift == filter.Shift.Value);
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
        if (searchText.Length > 0)
            children = children.Where(child => ArabicNameNormalizer.Normalize(child.FullName).Contains(searchText, StringComparison.Ordinal)).ToList();
        logger.LogDebug("Loaded children; resultCount={ResultCount}", children.Count);
        return Result.Success<IReadOnlyList<ChildDto>>(children.Adapt<List<ChildDto>>());
    }

    public async Task<Result<IReadOnlyList<ClassroomFilterOptionDto>>> GetClassroomsForFilterAsync(CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Students.View", ChildErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        var classrooms = await unitOfWork.Repository<Classroom>().ListAsync(
            classroom => classroom.IsActive,
            cancellationToken: cancellationToken);
        var result = classrooms
            .OrderBy(classroom => classroom.Name)
            .Select(classroom => new ClassroomFilterOptionDto(classroom.Id, classroom.Name))
            .ToList();
        return Result.Success<IReadOnlyList<ClassroomFilterOptionDto>>(result);
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

    public async Task<Result<ChildDto>> GetChildByIdAsync(Guid childId)
    {
        var permissionError = PermissionGuard.Check(permissions, "Students.View", ChildErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        var child = await unitOfWork.Repository<Child>().FirstOrDefaultAsync(c => c.Id == childId);
        if (child is null) return ChildErrors.NotFound;
        return Result.Success(child.Adapt<ChildDto>());
    }

    public async Task<Result<ChildDto>> GetChildByNameAsync(string childName)
    {
        var permissionError = PermissionGuard.Check(permissions, "Students.View", ChildErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        var child = await unitOfWork.Repository<Child>().FirstOrDefaultAsync(c => c.FullName == childName);
        if (child is null) return ChildErrors.NotFound;
        return Result.Success(child.Adapt<ChildDto>());
    }

    public async Task<Result<IReadOnlyList<ChildAttachmentDto>>> GetChildAttachmentsAsync(Guid childId, CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Students.View", ChildErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        if (!await unitOfWork.Repository<Child>().AnyAsync(x => x.Id == childId, cancellationToken)) return ChildErrors.NotFound;
        var items = await unitOfWork.Repository<FileAttachment>().ListAsync(x => x.ChildId == childId, cancellationToken: cancellationToken);
        return Result.Success<IReadOnlyList<ChildAttachmentDto>>(items.OrderBy(x => x.Type)
            .Select(x => new ChildAttachmentDto(x.Id, x.Type, x.OriginalFileName, x.RelativePath, x.ContentType, x.SizeInBytes)).ToArray());
    }

    public async Task<Result> AddChildAttachmentAsync(Guid childId, ChildAttachmentDto attachment, CancellationToken cancellationToken = default)
    {
        var permissionError = PermissionGuard.Check(permissions, "Students.Edit", ChildErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        if (attachment is null || !Enum.IsDefined(attachment.Type) || string.IsNullOrWhiteSpace(attachment.RelativePath)) return ChildErrors.InvalidData;
        if (!await unitOfWork.Repository<Child>().AnyAsync(x => x.Id == childId, cancellationToken)) return ChildErrors.NotFound;
        try
        {
            var repository = unitOfWork.Repository<FileAttachment>();
            var current = await repository.FirstOrDefaultAsync(x => x.ChildId == childId && x.Type == attachment.Type, asNoTracking: false, cancellationToken: cancellationToken);
            if (current is null)
                await repository.AddAsync(new FileAttachment { ChildId = childId, Type = attachment.Type, OriginalFileName = attachment.OriginalFileName, RelativePath = attachment.RelativePath, ContentType = attachment.ContentType, SizeInBytes = attachment.SizeInBytes });
            else
            {
                current.OriginalFileName = attachment.OriginalFileName;
                current.RelativePath = attachment.RelativePath;
                current.ContentType = attachment.ContentType;
                current.SizeInBytes = attachment.SizeInBytes;
            }
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success("تم حفظ الوثيقة.");
        }
        catch (DbUpdateException exception)
        {
            logger.LogWarning(exception, "Child attachment save failed; childId={ChildId}", childId);
            return PersistenceErrorMapper.FromException(exception);
        }
    }

    public async Task<Result<IReadOnlyList<ChildDto>>> GetChildrenByDateOfBirthAsync(DateOnly dateOfBirth)
    {
        var permissionError = PermissionGuard.Check(permissions, "Students.View", ChildErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        var children = await unitOfWork.Repository<Child>().ListAsync(c => c.DateOfBirth == dateOfBirth);
        return Result.Success<IReadOnlyList<ChildDto>>(children.Adapt<List<ChildDto>>());
    }

    public async Task<Result<IReadOnlyList<ChildDto>>> GetChildrenByGenderAsync(ChildGender gender)
    {
        var permissionError = PermissionGuard.Check(permissions, "Students.View", ChildErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        var children = await unitOfWork.Repository<Child>().ListAsync(c => c.Gender == gender);
        return Result.Success<IReadOnlyList<ChildDto>>(children.Adapt<List<ChildDto>>());
    }

    public async Task<Result<IReadOnlyList<ChildDto>>> GetChildrenByRangeOfEnrollmentDateAsync(DateOnly startDate, DateOnly endDate)
    {
        var permissionError = PermissionGuard.Check(permissions, "Students.View", ChildErrors.PermissionDenied);
        if (permissionError is not null) return permissionError;
        if (startDate > endDate) return ChildErrors.InvalidFilter;
        var children = await unitOfWork.Repository<Child>().ListAsync(c => c.EnrollmentDate >= startDate && c.EnrollmentDate <= endDate);
        return Result.Success<IReadOnlyList<ChildDto>>(children.Adapt<List<ChildDto>>());
    }

    private static bool ValidChildFields(CreateChildDTO x) =>
        !string.IsNullOrWhiteSpace(x.FullName) && !string.IsNullOrWhiteSpace(x.Address)
        && !string.IsNullOrWhiteSpace(x.FatherName) && !string.IsNullOrWhiteSpace(x.MotherName)
        && PhoneNumberValidator.IsValidEgyptianMobile(x.FatherPhone)
        && PhoneNumberValidator.IsValidEgyptianMobile(x.MotherPhone)
        && (string.IsNullOrWhiteSpace(x.FatherWhatsApp) || PhoneNumberValidator.IsValidEgyptianMobile(x.FatherWhatsApp))
        && (string.IsNullOrWhiteSpace(x.MotherWhatsApp) || PhoneNumberValidator.IsValidEgyptianMobile(x.MotherWhatsApp));

    private static bool ValidChildFields(UpdateChildDTO x) =>
        !string.IsNullOrWhiteSpace(x.FullName) && !string.IsNullOrWhiteSpace(x.Address)
        && !string.IsNullOrWhiteSpace(x.FatherName) && !string.IsNullOrWhiteSpace(x.MotherName)
        && PhoneNumberValidator.IsValidEgyptianMobile(x.FatherPhone)
        && PhoneNumberValidator.IsValidEgyptianMobile(x.MotherPhone)
        && (string.IsNullOrWhiteSpace(x.FatherWhatsApp) || PhoneNumberValidator.IsValidEgyptianMobile(x.FatherWhatsApp))
        && (string.IsNullOrWhiteSpace(x.MotherWhatsApp) || PhoneNumberValidator.IsValidEgyptianMobile(x.MotherWhatsApp));

    private static int CountActiveFilters(ChildFilter? filter)
    {
        if (filter is null) return 0;
        return new[]
        {
            !string.IsNullOrWhiteSpace(filter.SearchText), filter.Gender.HasValue, filter.Shift.HasValue,
            filter.DateOfBirthFrom.HasValue, filter.DateOfBirthTo.HasValue,
            filter.EnrollmentDateFrom.HasValue, filter.EnrollmentDateTo.HasValue,
            filter.ClassroomId.HasValue, filter.HasPaidSubscription.HasValue
        }.Count(active => active);
    }
}
