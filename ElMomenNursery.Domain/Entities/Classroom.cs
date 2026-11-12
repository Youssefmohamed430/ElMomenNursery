namespace ElMomenNursery.Domain.Entities;

public sealed class Classroom
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ChildClassEnrollment> ChildEnrollments { get; } = new List<ChildClassEnrollment>();
    public ICollection<ClassTeacherAssignment> TeacherAssignments { get; } = new List<ClassTeacherAssignment>();
}
