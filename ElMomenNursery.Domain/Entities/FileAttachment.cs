namespace ElMomenNursery.Domain.Entities;
using ElMomenNursery.Domain.Enums;

public sealed class FileAttachment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? ChildId { get; set; }
    public Guid? EmployeeId { get; set; }
    public AttachmentType Type { get; set; }
    public required string OriginalFileName { get; set; }
    public required string RelativePath { get; set; }
    public string? ContentType { get; set; }
    public long? SizeInBytes { get; set; }
    public string? Checksum { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public Child? Child { get; set; }
    public Employee? Employee { get; set; }
    public UserAccount? UploadedByUser { get; set; }
}
