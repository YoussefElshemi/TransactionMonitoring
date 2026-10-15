using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.SubOrganizations;

public record ListSubOrganizationRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required SubOrganizationName? Name { get; init; }
    public required IsEnabled? IsEnabled { get; init; }
}
