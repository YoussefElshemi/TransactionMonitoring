using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Rules;

public record RuleVersion
{
    public required RuleVersionId RuleVersionId { get; init; }
    public required RuleId RuleId { get; init; }
    public required VersionNumber VersionNumber { get; init; }
    public required RuleBody RuleBody { get; init; }
    public required RuleMatchPolarity MatchPolarity { get; init; }
    public required RuleAction Action { get; init; }
    public required RuleSeverity Severity { get; init; }
    public required RiskWeight RiskWeight { get; init; }
    public required RuleSetId RuleSetId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
    public required CreatedAt CreatedAt { get; init; }

    public required Mode Mode { get; init; }
}
