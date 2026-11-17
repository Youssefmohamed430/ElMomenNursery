namespace ElMomenNursery.Domain.Abstractions;

public static class ClassroomErrors
{
    public static readonly Error PermissionDenied = new("Classrooms.PermissionDenied", "لا تملك الصلاحية اللازمة لإدارة الفصول.");
    public static readonly Error NotFound = new("Classrooms.NotFound", "الفصل غير موجود.");
    public static readonly Error InvalidData = new("Classrooms.InvalidData", "بيانات الفصل غير مكتملة أو غير صحيحة.");
    public static readonly Error DuplicateName = new("Classrooms.DuplicateName", "اسم الفصل مستخدم بالفعل.");
    public static readonly Error PersistenceFailure = new("Classrooms.PersistenceFailure", "تعذر حفظ بيانات الفصل. أعد المحاولة.");
}
