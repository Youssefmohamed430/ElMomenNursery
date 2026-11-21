namespace ElMomenNursery.Domain.Abstractions;

public static class FinanceErrors
{
    public static readonly Error ExpenseNotFound = new("Finance.ExpenseNotFound", "المصروف المطلوب غير موجود أو تم إلغاؤه.");
    public static readonly Error IncomeNotFound = new("Finance.IncomeNotFound", "الإيراد المطلوب غير موجود.");
    public static readonly Error InvalidAmount = new("Finance.InvalidAmount", "المبلغ يجب أن يكون أكبر من الصفر.");
    public static readonly Error DescriptionRequired = new("Finance.DescriptionRequired", "بيان الحركة المالية مطلوب.");
    public static readonly Error CategoryRequired = new("Finance.CategoryRequired", "فئة المصروف مطلوبة.");
    public static readonly Error SourceRequired = new("Finance.SourceRequired", "مصدر الإيراد مطلوب.");
    public static readonly Error NonManualNotEditable = new("Finance.NonManualNotEditable", "لا يمكن تعديل أو حذف هذه الحركة المالية من شاشة الماليات لأنها مسجلة تلقائياً من شاشة أخرى.");
}

