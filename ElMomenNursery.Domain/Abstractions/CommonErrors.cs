namespace ElMomenNursery.Domain.Abstractions;

public static class CommonErrors
{
    public static readonly Error PermissionDenied = new("Common.PermissionDenied", "لا تملك الصلاحية اللازمة لإتمام هذه العملية.");
    public static readonly Error InvalidRequest = new(
        "Common.InvalidRequest",
        "البيانات المدخلة غير صالحة.");

    public static readonly Error NotFound = new(
        "Common.NotFound",
        "السجل المطلوب غير موجود.");

    public static readonly Error Conflict = new(
        "Common.Conflict",
        "يتعارض هذا الإجراء مع بيانات موجودة.");

    public static readonly Error InvalidCredentials = new(
        "Common.InvalidCredentials",
        "اسم المستخدم أو كلمة المرور غير صحيحة.");
}
