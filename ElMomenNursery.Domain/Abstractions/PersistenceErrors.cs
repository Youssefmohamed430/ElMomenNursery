namespace ElMomenNursery.Domain.Abstractions;

public static class PersistenceErrors
{
    public static readonly Error Unique = new("Persistence.Unique", "توجد بيانات مسجلة بالقيمة نفسها.");
    public static readonly Error ForeignKey = new("Persistence.ForeignKey", "لا يمكن إتمام العملية لارتباط السجل ببيانات أخرى.");
    public static readonly Error Check = new("Persistence.Check", "القيم المدخلة لا تحقق قواعد حفظ البيانات.");
    public static readonly Error Concurrency = new("Persistence.Concurrency", "تغيرت البيانات أثناء العملية. حدّث الشاشة ثم أعد المحاولة.");
    public static readonly Error Unknown = new("Persistence.Unknown", "تعذر حفظ البيانات. أعد المحاولة.");
}
