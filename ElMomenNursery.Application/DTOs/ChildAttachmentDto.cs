using ElMomenNursery.Domain.Enums;

namespace ElMomenNursery.Application.DTOs;

public sealed record ChildAttachmentDto(Guid Id, AttachmentType Type, string OriginalFileName, string RelativePath, string? ContentType, long? SizeInBytes);
