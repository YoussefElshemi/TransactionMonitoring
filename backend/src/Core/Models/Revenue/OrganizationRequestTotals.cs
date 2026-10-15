using Core.ValueObjects;

namespace Core.Models.Revenue;

public record OrganizationRequestTotals
{
    public required OrganizationId OrganizationId { get; init; }
    public required RequestCount TotalRequests { get; init; }
    public required RequestCount FailedRequests { get; init; }
}
