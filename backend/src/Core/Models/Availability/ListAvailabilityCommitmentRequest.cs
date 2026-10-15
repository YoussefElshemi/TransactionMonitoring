using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Availability;

public record ListAvailabilityCommitmentRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required IncludeSuperseded IncludeSuperseded { get; init; }
}
