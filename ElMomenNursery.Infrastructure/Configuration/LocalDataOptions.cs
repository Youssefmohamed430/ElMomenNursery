namespace ElMomenNursery.Infrastructure.Configuration;

public sealed class LocalDataOptions
{
    public const string SectionName = "LocalData";
    public string? DirectoryPath { get; init; }
}
