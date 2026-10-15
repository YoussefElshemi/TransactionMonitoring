using Core.ValueObjects;

namespace Core.Models.RuleSets;

public record UpdateRuleSetRequest
{
    public required RuleSetId RuleSetId { get; init; }
    public required FriendlyName? FriendlyName { get; init; }
    public required IsEnabled? IsEnabled { get; init; }
    public required ReviewThreshold? ReviewThreshold { get; init; }
    public required SubOrganizationId? SubOrganizationId { get; init; }

    public required ClearCriteria ClearSubOrganization { get; init; }
}