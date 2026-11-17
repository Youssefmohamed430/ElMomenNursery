namespace ElMomenNursery.Domain.Abstractions;

public static class ChildErrors
{
    public static readonly Error PermissionDenied = new("Children.PermissionDenied", "لا تملك الصلاحية اللازمة لإدارة سجلات الأطفال.");
    public static readonly Error NotFound = new("Children.NotFound", "لم يتم العثور على سجل الطفل.");
    public static readonly Error InvalidData = new("Children.InvalidData", "بيانات الطفل غير مكتملة أو غير صحيحة.");
    public static readonly Error InvalidFilter = new("Children.InvalidFilter", "نطاق تاريخ البحث غير صالح.");
    public static readonly Error PersistenceFailure = new("Children.PersistenceFailure", "تعذر حفظ بيانات الطفل. أعد المحاولة.");
    public static readonly Error Deleted = new("Children.Deleted", "تم نقل سجل الطفل إلى المحذوفات.");
    public static readonly Error Restored = new("Children.Restored", "تمت استعادة سجل الطفل.");
    public static readonly Error Created = new("Children.Created", "تم حفظ بيانات الطفل.");
    public static readonly Error Updated = new("Children.Updated", "تم تحديث بيانات الطفل.");
}
