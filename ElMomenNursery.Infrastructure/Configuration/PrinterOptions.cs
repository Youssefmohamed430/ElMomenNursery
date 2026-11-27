namespace ElMomenNursery.Infrastructure.Configuration;

public sealed class PrinterOptions
{
    public const string SectionName = "Printer";
    public string Name { get; init; } = string.Empty;
    public int PaperWidthMm { get; init; } = 80;
    public int PaperHeightMm { get; init; } = 297;
    public string FontName { get; init; } = "Arial";
    public float FontSize { get; init; } = 10f;
    public bool AutoCut { get; init; } = true;
}
