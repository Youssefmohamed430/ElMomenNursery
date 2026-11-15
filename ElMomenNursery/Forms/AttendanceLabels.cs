using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Forms;

internal static class AttendanceLabels
{
    public static string Status(AttendanceStatus status) => status switch
    {
        AttendanceStatus.Present => "حاضر", AttendanceStatus.Absent => "غائب",
        AttendanceStatus.Late => "متأخر", AttendanceStatus.Excused => "غياب بعذر", _ => "غير محدد"
    };
    public static string Method(AttendanceEntryMethod method) => method switch
    {
        AttendanceEntryMethod.Manual => "يدوي", AttendanceEntryMethod.BiometricDevice => "جهاز بصمة",
        AttendanceEntryMethod.BarcodeDevice => "قارئ باركود", AttendanceEntryMethod.OtherDevice => "جهاز آخر", _ => "غير محدد"
    };
}
