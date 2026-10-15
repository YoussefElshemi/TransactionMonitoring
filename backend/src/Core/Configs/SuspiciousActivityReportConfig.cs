namespace Core.Configs;

public record SuspiciousActivityReportConfig
{
    public int DefaultFilingDeadlineDays { get; init; } = 30;

    public int NarrativeNoteLimit { get; init; } = 25;
}
