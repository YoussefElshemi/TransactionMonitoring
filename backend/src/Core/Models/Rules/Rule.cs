using Core.Enums;
using Core.Models.BaseModels;
using Core.Models.Organizations;
using Core.ValueObjects;

namespace Core.Models.Rules;

public record Rule : DeletableModel
{
    public required RuleId RuleId { get; init; }
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
    public required Organization? Organization { get; init; }
    public required IsEnabled IsEnabled { get; init; }
    public required RuleSetId RuleSetId { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
    public required RuleVersionId CurrentRuleVersionId { get; init; }
    public required VersionNumber CurrentVersionNumber { get; init; }
    public required HasActiveDeployment HasActiveDeployment { get; init; }

    public required Mode Mode { get; init; }
}