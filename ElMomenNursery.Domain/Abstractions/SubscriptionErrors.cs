namespace ElMomenNursery.Domain.Abstractions;

public static class SubscriptionErrors
{
    public static readonly Error NotFound = new("Subscriptions.NotFound", "الاشتراك غير موجود.");
    public static readonly Error ChildNotFound = new("Subscriptions.ChildNotFound", "الطفل غير موجود.");
    public static readonly Error PlanNotFound = new("Subscriptions.PlanNotFound", "خطة الاشتراك غير موجودة.");
    public static readonly Error InvalidPeriod = new("Subscriptions.InvalidPeriod", "فترة الاشتراك غير صالحة.");
    public static readonly Error PeriodBeforeEnrollment = new("Subscriptions.PeriodBeforeEnrollment", "لا يمكن أن تبدأ فترة الاشتراك قبل تاريخ تسجيل الطفل.");
    public static readonly Error InvalidAmount = new("Subscriptions.InvalidAmount", "المبلغ يجب أن يكون صفرًا أو أكثر، والدفعة أكبر من صفر.");
    public static readonly Error AmountBelowPaid = new("Subscriptions.AmountBelowPaid", "لا يمكن أن يكون المستحق أقل من المبلغ المدفوع.");
    public static readonly Error OverpaymentNotAllowed = new("Subscriptions.OverpaymentNotAllowed", "مبلغ الدفعة أكبر من المتبقي على الاشتراك.");
    public static readonly Error AlreadyCancelled = new("Subscriptions.AlreadyCancelled", "الاشتراك ملغي بالفعل.");
    public static readonly Error PaymentNotAllowedForStatus = new("Subscriptions.PaymentNotAllowedForStatus", "لا يمكن تسجيل دفعة لاشتراك ملغي أو معفى.");
    public static readonly Error HasPayments = new("Subscriptions.HasPayments", "لا يمكن حذف اشتراك له دفعات أو تخصيصات مالية.");
    public static readonly Error PermissionDenied = new("Subscriptions.PermissionDenied", "لا تملك الصلاحية اللازمة لإتمام هذه العملية.");
    public static readonly Error ReasonRequired = new("Subscriptions.ReasonRequired", "سبب التعديل أو الإلغاء مطلوب.");
    public static readonly Error OverlappingPeriod = new("Subscriptions.OverlappingPeriod", "توجد فترة اشتراك متقاطعة لهذا الطفل. هل تريد المتابعة؟");
    public static readonly Error PersistenceFailure = new("Subscriptions.PersistenceFailure", "تعذر حفظ بيانات الاشتراك أو الدفعة. أعد المحاولة.");
}
