namespace Core.Models.Webhooks;

public record DecisionRuleWebhookMessage
{
    public required Guid RuleId { get; init; }
    public required Guid RuleVersionId { get; init; }
    public required string FriendlyName { get; init; }
    public required string Action { get; init; }
    public required string Severity { get; init; }
    public required int RiskWeight { get; init; }
}