using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Rules;

public record UpdateRuleRequest
{
    public required RuleId RuleId { get; init; }
    public required FriendlyName? FriendlyName { get; init; }
    public required IsEnabled? IsEnabled { get; init; }
    public required RuleAction? Action { get; init; }
    public required RuleSeverity? Severity { get; init; }
    public required RiskWeight? RiskWeight { get; init; }
    public required Patch<HoldWindow?> HoldWindow { get; init; }
    public required Patch<HoldExpiryVerdict?> HoldExpiryVerdict { get; init; }
    public required Patch<PartyRole?> AlertSubjectRole { get; init; }
    public required Patch<DeduplicationWindow?> DeduplicationWindow { get; init; }
}
