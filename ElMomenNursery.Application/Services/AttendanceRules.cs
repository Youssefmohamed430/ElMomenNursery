namespace ElMomenNursery.Application.Services;

public static class AttendanceRules
{
    public static Error? Validate(DateOnly date, AttendanceStatus status, DateTimeOffset? checkIn, DateTimeOffset? checkOut)
    {
        if (date == default || date > DateOnly.FromDateTime(DateTime.Today)) return AttendanceErrors.InvalidDate;

        if (!Enum.IsDefined(status)) return AttendanceErrors.InvalidStatus;

        if (checkIn.HasValue && DateOnly.FromDateTime(checkIn.Value.LocalDateTime) != date
            || checkOut.HasValue && DateOnly.FromDateTime(checkOut.Value.LocalDateTime) != date)
            return AttendanceErrors.InvalidTimes;

        if (checkIn.HasValue && checkOut.HasValue && checkOut.Value < checkIn.Value) return AttendanceErrors.CheckOutBeforeCheckIn;
        if (status is AttendanceStatus.Absent or AttendanceStatus.Excused)
            return checkIn.HasValue || checkOut.HasValue ? AttendanceErrors.InvalidTimes : null;
        if (!checkIn.HasValue) return AttendanceErrors.InvalidTimes;
        return null;
    }

    public static bool HasCorrectionReason(string? reason) => !string.IsNullOrWhiteSpace(reason);
}
