namespace Core.Configs;

public record AuditConfig
{
    public int ExportEntryLimit { get; init; } = 50_000;

    public int VerificationBatchSize { get; init; } = 1_000;
}
