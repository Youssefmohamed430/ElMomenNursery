namespace ElMomenNursery.Domain.Abstractions;

public static class AuthErrors
{
    public static readonly Error LockedOut = new("Auth.LockedOut", "تعذر تسجيل الدخول الآن. أعد المحاولة لاحقًا.");
    public static readonly Error WeakPassword = new("Auth.WeakPassword", "كلمة المرور يجب أن تتكون من 12 حرفًا على الأقل، وتشمل أحرفًا وأرقامًا.");
    public static readonly Error SetupAlreadyComplete = new("Auth.SetupAlreadyComplete", "تم إعداد حساب المسؤول بالفعل.");
    public static readonly Error SetupUnavailable = new("Auth.SetupUnavailable", "تعذر إكمال إعداد المسؤول. أعد تشغيل التطبيق أو تواصل مع المسؤول.");
}
