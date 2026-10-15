using Core.ValueObjects;

namespace Core.Models.RuleSets;

public record CreateRuleSetRequest
{
    public required FriendlyName FriendlyName { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required SubOrganizationId? SubOrganizationId { get; init; }
    public required IsEnabled IsEnabled { get; init; }
    public required ReviewThreshold? ReviewThreshold { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
}