using Core.ValueObjects;

namespace Core.Models.SubOrganizations;

public record UpdateSubOrganizationRequest
{
    public required SubOrganizationId SubOrganizationId { get; init; }
    public required SubOrganizationName? Name { get; init; }
    public required SubOrganizationDescription? Description { get; init; }
    public required ExternalReference? ExternalReference { get; init; }
    public required ClearCriteria ClearExternalReference { get; init; }
    public required IsEnabled? IsEnabled { get; init; }
}
