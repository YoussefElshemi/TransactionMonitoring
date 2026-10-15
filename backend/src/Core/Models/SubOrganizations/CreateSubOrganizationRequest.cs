using Core.ValueObjects;

namespace Core.Models.SubOrganizations;

public record CreateSubOrganizationRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required SubOrganizationName Name { get; init; }
    public required SubOrganizationDescription? Description { get; init; }
    public required ExternalReference? ExternalReference { get; init; }
    public required IsEnabled IsEnabled { get; init; }
}
