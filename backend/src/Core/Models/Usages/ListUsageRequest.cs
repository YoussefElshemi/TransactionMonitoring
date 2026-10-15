using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Usages;

public record ListUsageRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required Resource? Resource { get; init; }
}