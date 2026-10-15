namespace Core.Configs;

public record PaymentConfig
{
    public bool CollectionEnabled { get; init; }

    public int GraceDays { get; init; } = 3;

    public int[] NoticeIntervalDays { get; init; } = [0, 7, 14];

    public int RestrictionProposalDays { get; init; } = 14;

    public string ReceiptNumberPrefix { get; init; } = "REC";

    public string? ApiKey { get; init; }
    public string? WebhookSecret { get; init; }

    public int WebhookToleranceSeconds { get; init; } = 300;
}
