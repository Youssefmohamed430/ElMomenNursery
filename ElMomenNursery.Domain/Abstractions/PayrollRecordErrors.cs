namespace ElMomenNursery.Domain.Abstractions;

public static class PayrollRecordErrors
{
    public static readonly Error NotFound = new("Payroll.NotFound", "سجل الراتب غير موجود.");
    public static readonly Error PermissionDenied = new("Payroll.PermissionDenied", "لا تملك الصلاحية اللازمة لإدارة الرواتب.");
    public static readonly Error InvalidData = new("Payroll.InvalidData", "بيانات الراتب أو الفترة غير صحيحة.");
    public static readonly Error AlreadyPaid = new("Payroll.AlreadyPaid", "تم صرف هذا الراتب من قبل.");
    public static readonly Error NotCalculated = new("Payroll.NotCalculated", "احسب صافي الراتب قبل الصرف.");
    public static readonly Error CalculationFailed = new("Payroll.CalculationFailed", "تعذر حساب الراتب.");
    public static readonly Error PersistenceFailure = new("Payroll.PersistenceFailure", "تعذر حفظ سجل الراتب.");
    public static readonly Error InvalidAdvance = new("Payroll.InvalidAdvance", "بيانات السلفة غير صحيحة.");
    public static readonly Error AdvanceBalanceChanged = new("Payroll.AdvanceBalanceChanged", "تغير رصيد السلف منذ آخر حساب. أعد حساب الراتب قبل الصرف.");
    public static readonly Error CalculatedFinalSalary = new("Payroll.Calculated", "تم حساب صافي الراتب.");
}
