using Core.ValueObjects;

namespace Presentation.Factories;

public readonly record struct EnrichmentContext
{
    public required OrganizationId OrganizationId { get; init; }
    public required bool IsAdmin { get; init; }

    public bool CanDisclose(OrganizationId owningOrganizationId)
    {
        return IsAdmin || owningOrganizationId == OrganizationId;
    }
}
