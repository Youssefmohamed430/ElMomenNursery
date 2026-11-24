using ElMomenNursery.Application.Abstractions.Persistence;
using ElMomenNursery.Application.DTOs;
using ElMomenNursery.Application.Common;
using ElMomenNursery.Application.IServices;
using ElMomenNursery.Domain.Abstractions;
using ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ElMomenNursery.Application.Services;

public sealed class SubscriptionService : ISubscriptionService
{
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<SubscriptionService> logger;
    private readonly IPermissionService permissions;
    private readonly ISessionManager sessions;

    public SubscriptionService(IUnitOfWork unitOfWork, ILogger<SubscriptionService> logger,
        IPermissionService permissions, ISessionManager sessions)
    {
        this.unitOfWork = unitOfWork;
        this.logger = logger;
        this.permissions = permissions;
        this.sessions = sessions;
    }

    public async Task<Result<IReadOnlyList<SubscriptionDto>>> GetSubscriptionsAsync(SubscriptionFilter filter, CancellationToken ct = default)
    {
        if (!Allowed("Subscriptions.View")) return SubscriptionErrors.PermissionDenied;
        IQueryable<Subscription> q = unitOfWork.Repository<Subscription>().Query().Include(x => x.Child).Include(x => x.SubscriptionPlan);
        if (filter.ClassroomId is Guid classroomId)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            q = q.Where(x => x.Child.ClassEnrollments.Any(enrollment => enrollment.ClassroomId == classroomId &&
                enrollment.StartDate <= today && (enrollment.EndDate == null || enrollment.EndDate >= today)));
        }
        if (filter.Status is not null) q = q.Where(x => x.Status == filter.Status);
        if (filter.PlanId is Guid plan) q = q.Where(x => x.SubscriptionPlanId == plan);
        if (filter.PeriodFrom is DateOnly from) q = q.Where(x => x.PeriodEnd >= from);
        if (filter.PeriodTo is DateOnly to) q = q.Where(x => x.PeriodStart <= to);
        var rows = await q.ToListAsync(ct);
        var text = ArabicNameNormalizer.Normalize(filter.SearchText);
        if (text.Length > 0)
            rows = rows.Where(x => ArabicNameNormalizer.Normalize(x.Child.FullName).Contains(text, StringComparison.Ordinal)).ToList();
        var ids = rows.Select(x => x.Id).ToArray();
        var allocations = await unitOfWork.Repository<PaymentAllocation>().Query()
            .Where(x => ids.Contains(x.SubscriptionId) && x.Payment.Status == PaymentStatus.Posted)
            .Select(x => new { x.SubscriptionId, x.AmountApplied }).ToListAsync(ct);
        var paid = allocations.GroupBy(x => x.SubscriptionId).ToDictionary(g => g.Key, g => g.Sum(x => x.AmountApplied));
        var result = rows.Select(x => ToDto(x, paid.GetValueOrDefault(x.Id)))
            .Where(x => !filter.OnlyWithBalance || x.RemainingAmount > 0)
            .OrderBy(x => x.PeriodStart).ThenBy(x => x.ChildName, StringComparer.CurrentCulture).ToArray();
        return Result.Success<IReadOnlyList<SubscriptionDto>>(result);
    }

    public async Task<Result<IReadOnlyList<ClassroomFilterOptionDto>>> GetClassroomsForFilterAsync(CancellationToken cancellationToken = default)
    {
        if (!Allowed("Subscriptions.View")) return SubscriptionErrors.PermissionDenied;
        var rows = await unitOfWork.Repository<Classroom>().Query()
            .Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new ClassroomFilterOptionDto(x.Id, x.Name)).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ClassroomFilterOptionDto>>(rows);
    }

    public async Task<Result<SubscriptionDto>> GetSubscriptionByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (!Allowed("Subscriptions.View")) return SubscriptionErrors.PermissionDenied;
        var item = await GetTrackedSubscription(id, false, ct);
        if (item is null) return SubscriptionErrors.NotFound;
        return ToDto(item, await PaidAsync(id, ct));
    }

    public async Task<Result<IReadOnlyList<SubscriptionDto>>> GetSubscriptionsForChildAsync(Guid childId, CancellationToken ct = default)
    {
        if (!Allowed("Subscriptions.View")) return SubscriptionErrors.PermissionDenied;
        var rows = await unitOfWork.Repository<Subscription>().Query().Include(x => x.Child).Include(x => x.SubscriptionPlan).Where(x => x.ChildId == childId).ToListAsync(ct);
        var payments = await unitOfWork.Repository<PaymentAllocation>().Query().Where(x => rows.Select(s => s.Id).Contains(x.SubscriptionId) && x.Payment.Status == PaymentStatus.Posted).ToListAsync(ct);
        var paid = payments.GroupBy(x => x.SubscriptionId).ToDictionary(g => g.Key, g => g.Sum(x => x.AmountApplied));
        return Result.Success<IReadOnlyList<SubscriptionDto>>(rows.Select(x => ToDto(x, paid.GetValueOrDefault(x.Id))).ToArray());
    }

    public async Task<Result<IReadOnlyList<ChildLookupDto>>> GetChildLookupAsync(string? search, CancellationToken ct = default)
    {
        if (!Allowed("Subscriptions.View")) return SubscriptionErrors.PermissionDenied;
        var children = await unitOfWork.Repository<Child>().Query().OrderBy(x => x.FullName).ToListAsync(ct);
        var normalizedSearch = ArabicNameNormalizer.Normalize(search);
        var rows = children
            .Where(x => normalizedSearch.Length == 0 || ArabicNameNormalizer.Normalize(x.FullName).Contains(normalizedSearch, StringComparison.Ordinal))
            .Take(100)
            .Select(x => new ChildLookupDto(x.Id, x.FullName)).ToList();
        return Result.Success<IReadOnlyList<ChildLookupDto>>(rows);
    }

    public async Task<Result<IReadOnlyList<SubscriptionPlanOptionDto>>> GetPlansAsync(CancellationToken ct = default)
    {
        if (!Allowed("Subscriptions.View")) return SubscriptionErrors.PermissionDenied;
        var rows = await unitOfWork.Repository<SubscriptionPlan>().Query().Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new SubscriptionPlanOptionDto(x.Id, x.Name, x.DefaultFee)).ToListAsync(ct);
        return Result.Success<IReadOnlyList<SubscriptionPlanOptionDto>>(rows);
    }

    public async Task<Result<SubscriptionDto>> CreateSubscriptionAsync(CreateSubscriptionDto dto, CancellationToken ct = default)
    {
        if (!Allowed("Subscriptions.Manage")) return SubscriptionErrors.PermissionDenied;

        if (dto.InitialPayment is not null && !Allowed("Payments.Create")) return SubscriptionErrors.PermissionDenied;

        if (dto.PeriodStart == default || dto.PeriodEnd < dto.PeriodStart) return SubscriptionErrors.InvalidPeriod;

        var child = await unitOfWork.Repository<Child>().FirstOrDefaultAsync(x => x.Id == dto.ChildId, asNoTracking: false, cancellationToken: ct);

        if (child is null) return SubscriptionErrors.ChildNotFound;

        if (dto.PeriodStart < child.EnrollmentDate) return SubscriptionErrors.PeriodBeforeEnrollment;

        SubscriptionPlan? plan = null;
        if (dto.SubscriptionPlanId is Guid planId)
        {
            plan = await unitOfWork.Repository<SubscriptionPlan>().FirstOrDefaultAsync(x => x.Id == planId, cancellationToken: ct);
            if (plan is null) return SubscriptionErrors.PlanNotFound;
        }
        var amount = dto.AmountDue ?? plan?.DefaultFee ?? 0m;
        if (amount < 0) return SubscriptionErrors.InvalidAmount;

        if (dto.InitialPayment is not null && (dto.InitialPayment.Amount <= 0 || dto.InitialPayment.Amount > amount))
            return dto.InitialPayment.Amount <= 0 ? SubscriptionErrors.InvalidAmount : SubscriptionErrors.OverpaymentNotAllowed;

        var overlap = await unitOfWork.Repository<Subscription>().AnyAsync(x => x.ChildId == child.Id && x.Status != SubscriptionStatus.Cancelled
            && x.PeriodStart <= dto.PeriodEnd && x.PeriodEnd >= dto.PeriodStart, ct);

        if (overlap && !dto.ConfirmOverlappingPeriod) return SubscriptionErrors.OverlappingPeriod;
        var initialPaid = dto.InitialPayment?.Amount ?? 0;

        var sub = new Subscription { ChildId = child.Id, Child = child, SubscriptionPlanId = plan?.Id, SubscriptionPlan = plan,
            PeriodStart = dto.PeriodStart, PeriodEnd = dto.PeriodEnd, AmountDue = amount, Notes = Clean(dto.Notes), Status = ComputeStatus(amount, initialPaid) };

        var before = "—";
        Payment? initialPayment = null;
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await unitOfWork.Repository<Subscription>().AddAsync(sub, ct);
            if (dto.InitialPayment is not null)
            {
                initialPayment = await CreatePayment(sub, dto.InitialPayment.Amount, dto.InitialPayment.PaymentMethod, dto.InitialPayment.Reference, null, ct);
                await Audit("Payment.Recorded", initialPayment.Id, "مدفوع سابقًا: 0", SubscriptionValues(sub, dto.InitialPayment.Amount), null, ct, nameof(Payment));
            }
            await Audit("Subscription.Created", sub.Id, before, SubscriptionValues(sub, dto.InitialPayment?.Amount ?? 0), null, ct);
            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None); throw;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            logger.LogError(ex, "Subscription creation failed; subscriptionId={SubscriptionId}; childId={ChildId}", sub.Id, child.Id);
            return PersistenceErrorMapper.FromException(ex);
        }

        sub.Child = child; sub.SubscriptionPlan = plan;
        var createdDto = ToDto(sub, dto.InitialPayment?.Amount ?? 0);
        if (initialPayment is not null) createdDto.InitialPaymentReceipt = await WithNurserySettings(ToReceipt(initialPayment, sub, 0, initialPayment.Amount), ct);
        return Result.Success(createdDto, "تم إنشاء الاشتراك.");
    }

    public async Task<Result<SubscriptionDto>> UpdateSubscriptionAsync(Guid id, UpdateSubscriptionDto dto, CancellationToken ct = default)
    {
        if (!Allowed("Subscriptions.Manage")) return SubscriptionErrors.PermissionDenied;
        if (string.IsNullOrWhiteSpace(dto.Reason)) return SubscriptionErrors.ReasonRequired;
        if (dto.PeriodStart == default || dto.PeriodEnd < dto.PeriodStart || dto.AmountDue < 0) return dto.AmountDue < 0 ? SubscriptionErrors.InvalidAmount : SubscriptionErrors.InvalidPeriod;
        var item = await GetTrackedSubscription(id, true, ct); if (item is null) return SubscriptionErrors.NotFound;
        var paid = await PaidAsync(id, ct); if (dto.AmountDue < paid) return SubscriptionErrors.AmountBelowPaid;
        SubscriptionPlan? updatedPlan = null;
        if (dto.SubscriptionPlanId is Guid planId)
        { updatedPlan = await unitOfWork.Repository<SubscriptionPlan>().FirstOrDefaultAsync(x => x.Id == planId, asNoTracking: false, cancellationToken: ct); if (updatedPlan is null) return SubscriptionErrors.PlanNotFound; }
        var before = SubscriptionValues(item, paid);
        item.SubscriptionPlanId = dto.SubscriptionPlanId; item.SubscriptionPlan = updatedPlan; item.PeriodStart = dto.PeriodStart; item.PeriodEnd = dto.PeriodEnd;
        item.AmountDue = dto.AmountDue; item.Notes = Clean(dto.Notes);
        if (item.Status is not (SubscriptionStatus.Cancelled or SubscriptionStatus.Waived)) item.Status = ComputeStatus(dto.AmountDue, paid);
        unitOfWork.Repository<Subscription>().Update(item);
        await Audit("Subscription.Updated", id, before, SubscriptionValues(item, paid), dto.Reason, ct);
        var saveError = await SaveChangesErrorAsync(ct);
        if (saveError is not null) return saveError;
        return Result.Success(ToDto(item, paid), "تم تحديث الاشتراك.");
    }

    public async Task<Result> CancelSubscriptionAsync(Guid id, string reason, CancellationToken ct = default)
    {
        if (!Allowed("Subscriptions.Manage")) return SubscriptionErrors.PermissionDenied;
        if (string.IsNullOrWhiteSpace(reason)) return SubscriptionErrors.ReasonRequired;
        var item = await GetTrackedSubscription(id, true, ct); if (item is null) return SubscriptionErrors.NotFound;
        if (item.Status == SubscriptionStatus.Cancelled) return SubscriptionErrors.AlreadyCancelled;
        var paid = await PaidAsync(id, ct); var before = SubscriptionValues(item, paid);
        item.Status = SubscriptionStatus.Cancelled; unitOfWork.Repository<Subscription>().Update(item);
        await Audit("Subscription.Cancelled", id, before, SubscriptionValues(item, paid), reason, ct);
        var saveError = await SaveChangesErrorAsync(ct);
        if (saveError is not null) return saveError;
        return Result.Success("تم إلغاء الاشتراك.");
    }

    public async Task<Result> DeleteSubscriptionAsync(Guid id, CancellationToken ct = default)
    {
        if (!Allowed("Subscriptions.Manage")) return SubscriptionErrors.PermissionDenied;
        var item = await GetTrackedSubscription(id, true, ct); if (item is null) return SubscriptionErrors.NotFound;
        if (await unitOfWork.Repository<PaymentAllocation>().AnyAsync(x => x.SubscriptionId == id, ct)
            || await unitOfWork.Repository<Payment>().AnyAsync(x => x.SubscriptionId == id, ct)) return SubscriptionErrors.HasPayments;
        await Audit("Subscription.Deleted", id, SubscriptionValues(item, 0), "تم الحذف", null, ct);
        unitOfWork.Repository<Subscription>().Remove(item);
        var saveError = await SaveChangesErrorAsync(ct);
        if (saveError is not null) return saveError;
        return Result.Success("تم حذف الاشتراك.");
    }

    public async Task<Result<PaymentReceiptDto>> RecordPaymentAsync(Guid subscriptionId, RecordPaymentDto dto, CancellationToken ct = default)
    {
        if (!Allowed("Payments.Create")) return SubscriptionErrors.PermissionDenied;

        if (dto.Amount <= 0) return SubscriptionErrors.InvalidAmount;

        var item = await GetTrackedSubscription(subscriptionId, true, ct);

        if (item is null) return SubscriptionErrors.NotFound;

        if (item.Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Waived) return SubscriptionErrors.PaymentNotAllowedForStatus;

        var beforePaid = await PaidAsync(subscriptionId, ct);

        if (dto.Amount > item.AmountDue - beforePaid) return SubscriptionErrors.OverpaymentNotAllowed;

        var before = SubscriptionValues(item, beforePaid);

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var payment = await CreatePayment(item, dto.Amount, dto.PaymentMethod, dto.Reference, dto.Notes, ct);
            var afterPaid = beforePaid + dto.Amount; item.Status = ComputeStatus(item.AmountDue, afterPaid);
            await Audit("Payment.Recorded", payment.Id, before, SubscriptionValues(item, afterPaid), dto.Reason, ct, nameof(Payment));
            await unitOfWork.CommitTransactionAsync(ct);
            return Result.Success(ToReceipt(payment, item, beforePaid, afterPaid), "تم تسجيل الدفعة.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None); throw;
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            logger.LogError(ex, "Payment recording failed; subscriptionId={SubscriptionId}", subscriptionId);
            return PersistenceErrorMapper.FromException(ex);
        }
    }

    public async Task<Result<PaymentReceiptDto>> GetReceiptAsync(Guid paymentId, CancellationToken ct = default)
    {
        if (!Allowed("Payments.View")) return SubscriptionErrors.PermissionDenied;
        var payment = await unitOfWork.Repository<Payment>().Query().Include(x => x.Child).Include(x => x.Subscription!).ThenInclude(x => x.SubscriptionPlan).Include(x => x.ReceivedByUser).FirstOrDefaultAsync(x => x.Id == paymentId, ct);
        if (payment?.Subscription is null) return SubscriptionErrors.NotFound;
        var applied = await unitOfWork.Repository<PaymentAllocation>().Query().Where(x => x.SubscriptionId == payment.SubscriptionId && x.Payment.Status == PaymentStatus.Posted)
            .Select(x => new { x.PaymentId, x.AmountApplied, x.Payment.PaidAt }).ToListAsync(ct);
        var paidBefore = applied.Where(x => x.PaidAt < payment.PaidAt || x.PaidAt == payment.PaidAt && x.PaymentId.CompareTo(payment.Id) < 0).Sum(x => x.AmountApplied);
        var paidNow = applied.Where(x => x.PaymentId == paymentId).Sum(x => x.AmountApplied);
        return await WithNurserySettings(ToReceipt(payment, payment.Subscription, paidBefore, paidBefore + paidNow), ct);
    }

    public async Task<Result<IReadOnlyList<PaymentReceiptDto>>> GetPaymentsForSubscriptionAsync(Guid id, CancellationToken ct = default)
    {
        if (!Allowed("Payments.View")) return SubscriptionErrors.PermissionDenied;
        var item = await GetTrackedSubscription(id, false, ct); if (item is null) return SubscriptionErrors.NotFound;
        var payments = await unitOfWork.Repository<Payment>().Query().Include(x => x.Child).Include(x => x.ReceivedByUser).Include(x => x.Allocations)
            .Where(x => x.SubscriptionId == id).OrderByDescending(x => x.PaidAt).ToListAsync(ct);
        decimal paid = 0; var rows = new List<PaymentReceiptDto>();
        foreach (var payment in payments.Where(x => x.Status == PaymentStatus.Posted).OrderBy(x => x.PaidAt))
        { rows.Add(ToReceipt(payment, item, paid, paid + payment.Amount)); paid += payment.Amount; }
        var settings = await unitOfWork.Repository<AppSetting>().Query().Where(x => x.Key == "Nursery.Name" || x.Key == "Nursery.Address" || x.Key == "Nursery.Phone").ToListAsync(ct);
        return Result.Success<IReadOnlyList<PaymentReceiptDto>>(rows.OrderByDescending(x => x.PaidAt).Select(x => WithNurserySettings(x, settings)).ToArray());
    }

    private bool Allowed(string code) => PermissionGuard.Check(permissions, code) is null;
    private async Task<Error?> SaveChangesErrorAsync(CancellationToken ct)
    {
        try { await unitOfWork.SaveChangesAsync(ct); return null; }
        catch (DbUpdateException exception)
        {
            var error = PersistenceErrorMapper.FromException(exception);
            logger.LogWarning(exception, "Subscription operation failed during persistence; errorCode={ErrorCode}", error.Code);
            return error;
        }
    }
    private Task<Subscription?> GetTrackedSubscription(Guid id, bool tracked, CancellationToken ct) => unitOfWork.Repository<Subscription>().FirstOrDefaultAsync(x => x.Id == id, asNoTracking: !tracked,
        includeProperties: [nameof(Subscription.Child), nameof(Subscription.SubscriptionPlan)], cancellationToken: ct);
    private async Task<decimal> PaidAsync(Guid id, CancellationToken ct) => (await unitOfWork.Repository<PaymentAllocation>().Query().Where(x => x.SubscriptionId == id && x.Payment.Status == PaymentStatus.Posted).Select(x => x.AmountApplied).ToListAsync(ct)).Sum();

    private async Task<Payment> CreatePayment(Subscription sub, decimal amount, PaymentMethod method, string? reference, string? notes, CancellationToken ct)
    {
        var now = DateTimeOffset.Now; var year = now.Year; var key = $"Receipts.Sequence.{year}";
        var setting = await unitOfWork.Repository<AppSetting>().FirstOrDefaultAsync(x => x.Key == key, asNoTracking: false, cancellationToken: ct);
        var sequence = setting is null ? 1 : int.Parse(setting.Value, System.Globalization.CultureInfo.InvariantCulture) + 1;
        if (setting is null) await unitOfWork.Repository<AppSetting>().AddAsync(new AppSetting { Key = key, Value = sequence.ToString(System.Globalization.CultureInfo.InvariantCulture), Description = "عداد أرقام الإيصالات السنوي." }, ct);
        else setting.Value = sequence.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var payment = new Payment { ChildId = sub.ChildId, Child = sub.Child, SubscriptionId = sub.Id, Subscription = sub, Amount = amount,
            PaidAt = now, PaymentMethod = method, Reference = Clean(reference), Notes = Clean(notes), ReceivedByUserId = sessions.CurrentUser?.UserId,
            ReceiptNumber = $"R-{year}-{sequence:000000}" };
        await unitOfWork.Repository<Payment>().AddAsync(payment, ct);
        var allocation = new PaymentAllocation { Payment = payment, PaymentId = payment.Id, Subscription = sub, SubscriptionId = sub.Id, AmountApplied = amount };
        await unitOfWork.Repository<PaymentAllocation>().AddAsync(allocation, ct);
        return payment;
    }

    private async Task Audit(string action, Guid id, string before, string after, string? reason, CancellationToken ct, string entityType = nameof(Subscription))
    {
        await unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog { Action = action, EntityType = entityType, EntityId = id.ToString("D"),
            UserAccountId = sessions.CurrentUser?.UserId, ChangeSummary = $"قبل: {before}; بعد: {after}; السبب: {reason ?? "—"}; المستخدم: {sessions.CurrentUser?.FullName ?? "—"}" }, ct);
    }

    private async Task<PaymentReceiptDto> WithNurserySettings(PaymentReceiptDto receipt, CancellationToken ct)
    {
        var settings = await unitOfWork.Repository<AppSetting>().Query().Where(x => x.Key == "Nursery.Name" || x.Key == "Nursery.Address" || x.Key == "Nursery.Phone").ToListAsync(ct);
        return WithNurserySettings(receipt, settings);
    }
    private static PaymentReceiptDto WithNurserySettings(PaymentReceiptDto receipt, IReadOnlyCollection<AppSetting> settings) => receipt with
    { NurseryName = settings.FirstOrDefault(x => x.Key == "Nursery.Name")?.Value ?? "حضانة المؤمن",
      NurseryAddress = settings.FirstOrDefault(x => x.Key == "Nursery.Address")?.Value,
      NurseryPhone = settings.FirstOrDefault(x => x.Key == "Nursery.Phone")?.Value };

    private static string SubscriptionValues(Subscription s, decimal paid) => $"childId={s.ChildId};planId={s.SubscriptionPlanId};period={s.PeriodStart:yyyy-MM-dd}..{s.PeriodEnd:yyyy-MM-dd};due={s.AmountDue};paid={paid};status={s.Status}";
    private static SubscriptionDto ToDto(Subscription s, decimal paid) => new() { Id = s.Id, ChildId = s.ChildId, ChildName = s.Child.FullName,
        SubscriptionPlanId = s.SubscriptionPlanId, SubscriptionPlanName = s.SubscriptionPlan?.Name, PeriodStart = s.PeriodStart, PeriodEnd = s.PeriodEnd,
        AmountDue = s.AmountDue, PaidAmount = paid, RemainingAmount = Math.Max(0, s.AmountDue - paid), Status = s.Status, Notes = s.Notes, CreatedAt = s.CreatedAt };
    private PaymentReceiptDto ToReceipt(Payment p, Subscription s, decimal paidBefore, decimal paidAfter) => new(p.Id, p.ReceiptNumber ?? string.Empty,
        p.PaidAt, p.Child.FullName, s.SubscriptionPlan?.Name ?? "اشتراك", s.PeriodStart, s.PeriodEnd, s.AmountDue, paidBefore, p.Amount,
        Math.Max(0, s.AmountDue - paidAfter), p.PaymentMethod, p.Reference, p.ReceivedByUser?.FullName ?? (p.ReceivedByUserId == sessions.CurrentUser?.UserId ? sessions.CurrentUser?.FullName : "—") ?? "—", p.Notes);
    public static SubscriptionStatus ComputeStatus(decimal due, decimal paid) => SubscriptionRules.ComputeStatus(due, paid);
    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
