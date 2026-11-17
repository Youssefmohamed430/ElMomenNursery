namespace ElMomenNursery.Domain.Entities;

public sealed class ChildClassEnrollment : ISoftDeletable
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public Guid ChildId { get; set; }
    public Guid ClassroomId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public required Child Child { get; set; }
    public required Classroom Classroom { get; set; }
}
