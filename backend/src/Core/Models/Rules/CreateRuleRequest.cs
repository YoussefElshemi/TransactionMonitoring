using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Rules;

public record CreateRuleRequest
{
    public required FriendlyName FriendlyName { get; init; }
    public required RuleBody RuleBody { get; init; }
    public required RuleMatchPolarity MatchPolarity { get; init; }
    public required RuleAction Action { get; init; }
    public required RuleSeverity Severity { get; init; }
    public required RiskWeight RiskWeight { get; init; }
    public required HoldWindow? HoldWindow { get; init; }
    public required HoldExpiryVerdict? HoldExpiryVerdict { get; init; }
    public required PartyRole? AlertSubjectRole { get; init; }
    public required DeduplicationWindow? DeduplicationWindow { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required IsEnabled IsEnabled { get; init; }
    public required RuleSetId RuleSetId { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
}