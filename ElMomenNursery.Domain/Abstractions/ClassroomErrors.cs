namespace ElMomenNursery.Domain.Abstractions;

public static class ClassroomErrors
{
    public static readonly Error PermissionDenied = new("Classes.PermissionDenied", "لا تملك الصلاحية اللازمة لإدارة الفصول.");
    public static readonly Error NotFound = new("Classes.NotFound", "لم يتم العثور على الفصل.");
    public static readonly Error InvalidData = new("Classes.InvalidData", "أدخل اسم الفصل واختر مدرسًا صحيحًا.");
    public static readonly Error PersistenceFailure = new("Classes.PersistenceFailure", "تعذر حفظ بيانات الفصل. أعد المحاولة.");
}
