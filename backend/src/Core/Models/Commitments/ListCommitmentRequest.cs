using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Commitments;

public record ListCommitmentRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required IncludeExpired IncludeExpired { get; init; }
}
