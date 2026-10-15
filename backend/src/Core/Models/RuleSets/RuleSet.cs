using Core.Enums;
using Core.Models.BaseModels;
using Core.Models.Rules;
using Core.ValueObjects;

namespace Core.Models.RuleSets;

public record RuleSet : DeletableModel
{
    public required RuleSetId RuleSetId { get; init; }
    public required FriendlyName FriendlyName { get; init; }
    public required OrganizationId OrganizationId { get; init; }

    public required SubOrganizationId? SubOrganizationId { get; init; }
    public required IsEnabled IsEnabled { get; init; }
    public required ReviewThreshold? ReviewThreshold { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
    public required RuleCount RuleCount { get; init; }
    public required List<Rule> Rules { get; init; }

    public required Mode Mode { get; init; }
}