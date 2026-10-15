using Core.ValueObjects;

namespace Core.Models.Permissions;

public record ResourceScope
{
    public required OrganizationId OrganizationId { get; init; }
    public required SubOrganizationId? SubOrganizationId { get; init; }
    public required RuleSetId? RuleSetId { get; init; }
    public required QueueId? QueueId { get; init; }

    public static ResourceScope Organization(OrganizationId organizationId)
    {
        return new ResourceScope
        {
            OrganizationId = organizationId,
            SubOrganizationId = null,
            RuleSetId = null,
            QueueId = null
        };
    }
}
