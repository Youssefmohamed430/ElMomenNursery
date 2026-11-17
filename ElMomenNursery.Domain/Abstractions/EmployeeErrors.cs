namespace ElMomenNursery.Domain.Abstractions;

public static class EmployeeErrors
{
    public static readonly Error PermissionDenied = new("Employees.PermissionDenied", "لا تملك الصلاحية اللازمة لإدارة سجلات الموظفين.");
    public static readonly Error NotFound = new("Employees.NotFound", "لم يتم العثور على سجل الموظف.");
    public static readonly Error InvalidData = new("Employees.InvalidData", "بيانات الموظف غير مكتملة أو غير صحيحة.");
    public static readonly Error InvalidSalary = new("Employees.InvalidSalary", "الراتب يجب أن يكون صفرًا أو أكثر.");
    public static readonly Error DuplicateNationalId = new("Employees.DuplicateNationalId", "الرقم القومي مسجل لموظف آخر.");
    public static readonly Error PersistenceFailure = new("Employees.PersistenceFailure", "تعذر حفظ بيانات الموظف. أعد المحاولة.");
    public static readonly Error Created = new("Employees.Created", "تم حفظ بيانات الموظف.");
    public static readonly Error Updated = new("Employees.Updated", "تم تحديث بيانات الموظف.");
    public static readonly Error Deleted = new("Employees.Deleted", "تم نقل سجل الموظف إلى المحذوفات.");
    public static readonly Error Restored = new("Employees.Restored", "تمت استعادة سجل الموظف.");
}
