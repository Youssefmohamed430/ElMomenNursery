namespace ElMomenNursery.Domain.Abstractions;

public static class AccountErrors
{
    public static readonly Error PermissionDenied = new("Accounts.PermissionDenied", "لا تملك الصلاحية اللازمة لإدارة الحسابات.");
    public static readonly Error NotFound = new("Accounts.NotFound", "الحساب أو الدور المطلوب غير موجود.");
    public static readonly Error DuplicateUsername = new("Accounts.DuplicateUsername", "اسم المستخدم مستخدم بالفعل.");
    public static readonly Error WeakPassword = new("Accounts.WeakPassword", "كلمة المرور يجب أن تتكون من 12 حرفًا على الأقل.");
    public static readonly Error LastAdministrator = new("Accounts.LastAdministrator", "يجب إبقاء حساب مسؤول نشط واحد على الأقل.");
    public static readonly Error SystemRoleProtected = new("Accounts.SystemRoleProtected", "لا يمكن تعديل صلاحيات الدور الأساسي.");
    public static readonly Error InvalidRequest = new("Accounts.InvalidRequest", "راجع البيانات المحددة ثم أعد المحاولة.");
    public static readonly Error PersistenceFailure = new("Accounts.PersistenceFailure", "تعذر حفظ تغييرات الحساب. أعد المحاولة.");
    public static readonly Error Created = new("Accounts.Created", "تم إنشاء الحساب.");
    public static readonly Error Updated = new("Accounts.Updated", "تم حفظ تغييرات الحساب.");
}
