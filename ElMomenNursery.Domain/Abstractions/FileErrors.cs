namespace ElMomenNursery.Domain.Abstractions;

public static class FileErrors
{
    public static readonly Error NotWritable = new("Files.NotWritable", "تعذر الكتابة إلى المجلد المحدد. اختر مجلدًا آخر ثم أعد المحاولة.");
    public static readonly Error FileInUse = new("Files.InUse", "الملف مفتوح في برنامج آخر. أغلقه ثم أعد المحاولة.");
    public static readonly Error Unknown = new("Files.Unknown", "تعذر إنشاء الملف. أعد المحاولة.");
}
