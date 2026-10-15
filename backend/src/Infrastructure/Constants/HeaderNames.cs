namespace Infrastructure.Constants;

public static class HeaderNames
{
    public const string Signature = "X-Signature";
    public const string CorrelationId = "X-Correlation-ID";

    public const string WebhookEvent = "X-Webhook-Event";
    public const string WebhookVersion = "X-Webhook-Version";
    public const string WebhookReplayOf = "X-Webhook-Replay-Of";
}