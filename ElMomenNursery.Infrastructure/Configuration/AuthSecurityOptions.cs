namespace ElMomenNursery.Infrastructure.Configuration;

public sealed class AuthSecurityOptions
{
    public const string SectionName = "Authentication";
    public int MaxFailedAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
}
