namespace ElMomenNursery.Domain.Entities;

public sealed class ClassTeacherAssignment : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid ClassroomId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public required Employee Employee { get; set; }
    public required Classroom Classroom { get; set; }
}
