using Environment = Core.Enums.Environment;

namespace Core.Configs;

public record AppConfig
{
    public string Version { get; init; } = null!;
    public Environment Environment { get; init; }
    public ObservabilityConfig ObservabilityConfig { get; init; } = null!;
    public BackgroundServiceConfig BackgroundServiceConfig { get; init; } = null!;
    public InvoiceConfig InvoiceConfig { get; init; } = null!;
    public IdempotencyConfig IdempotencyConfig { get; init; } = null!;
    public RuleConfig RuleConfig { get; init; } = null!;
    public DecisionConfig DecisionConfig { get; init; } = null!;
    public AlertConfig AlertConfig { get; init; } = new();

    public CaseConfig CaseConfig { get; init; } = new();

    public AttachmentConfig AttachmentConfig { get; init; } = new();

    public NotificationConfig NotificationConfig { get; init; } = new();

    public SuspiciousActivityReportConfig SuspiciousActivityReportConfig { get; init; } = new();

    public PayloadProtectionConfig PayloadProtectionConfig { get; init; } = new();

    public SandboxConfig SandboxConfig { get; init; } = new();

    public AuditConfig AuditConfig { get; init; } = new();

    public ExchangeRateFeedConfig ExchangeRateFeedConfig { get; init; } = new();

    public RateLimitConfig RateLimitConfig { get; init; } = new();

    public StatusConfig StatusConfig { get; init; } = new();

    public LocalizationConfig LocalizationConfig { get; init; } = new();

    public InvitationConfig InvitationConfig { get; init; } = new();

    public WebhookConfig WebhookConfig { get; init; } = new();

    public RevenueConfig RevenueConfig { get; init; } = new();

    public PaymentConfig PaymentConfig { get; init; } = new();
}