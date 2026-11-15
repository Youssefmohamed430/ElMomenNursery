namespace ElMomenNursery.Domain.Abstractions;

public static class AttendanceErrors
{
    public static readonly Error InvalidDate = new("Attendance.InvalidDate", "تاريخ الحضور غير صالح أو مستقبلي.");
    public static readonly Error InvalidStatus = new("Attendance.InvalidStatus", "حالة الحضور غير معروفة.");
    public static readonly Error InvalidTimes = new("Attendance.InvalidTimes", "أوقات الحضور والانصراف لا تتوافق مع الحالة.");
    public static readonly Error CheckOutBeforeCheckIn = new("Attendance.CheckOutBeforeCheckIn", "وقت الانصراف لا يمكن أن يسبق وقت الحضور.");
    public static readonly Error AlreadyRecorded = new("Attendance.AlreadyRecorded", "تم تسجيل الحضور لهذا الشخص في هذا التاريخ بالفعل.");
    public static readonly Error NotFound = new("Attendance.NotFound", "سجل الحضور غير موجود.");
    public static readonly Error PersonNotFound = new("Attendance.PersonNotFound", "الشخص المطلوب غير موجود.");
    public static readonly Error CorrectionReasonRequired = new("Attendance.CorrectionReasonRequired", "سبب التصحيح مطلوب.");
    public static readonly Error CheckOutUnavailable = new("Attendance.CheckOutUnavailable", "لا يوجد سجل حضور مفتوح لتسجيل الانصراف.");
    public static readonly Error InvalidRange = new("Attendance.InvalidRange", "نطاق التاريخ غير صالح.");
    public static readonly Error PermissionDenied = new("Attendance.PermissionDenied", "لا تملك الصلاحية اللازمة لإتمام هذه العملية.");
}
