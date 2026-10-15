using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.VelocityProfiles;

public record ListVelocityBackfillRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
}
