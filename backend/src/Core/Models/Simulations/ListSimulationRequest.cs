using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Simulations;

public record ListSimulationRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
}
