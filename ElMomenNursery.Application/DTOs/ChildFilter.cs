using ElMomenNursery.Domain.Enums;
using ElMomenNursery.Domain.Entities;

namespace ElMomenNursery.Application.DTOs;

/// <summary>
/// Optional criteria for the children list. Supplying more criteria narrows
/// the result by requiring every supplied criterion to match.
/// </summary>
public sealed class ChildFilter
{
    public string? SearchText { get; init; }
    public ChildGender? Gender { get; init; }
    public Shift? Shift { get; init; }
    public DateOnly? DateOfBirthFrom { get; init; }
    public DateOnly? DateOfBirthTo { get; init; }
    public DateOnly? EnrollmentDateFrom { get; init; }
    public DateOnly? EnrollmentDateTo { get; init; }
    public Guid? ClassroomId { get; init; }
    /// <summary>
    /// True selects children with a fully paid subscription active today;
    /// false selects children without one; null leaves payment status unfiltered.
    /// </summary>
    public bool? HasPaidSubscription { get; init; }
}
