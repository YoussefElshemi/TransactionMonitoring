using Core.ValueObjects;

namespace Core.Models.Usages;

public record SumUsageByDateRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required Month Month { get; init; }
    public required Year Year { get; init; }
}