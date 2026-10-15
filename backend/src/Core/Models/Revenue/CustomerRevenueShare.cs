using Core.ValueObjects;

namespace Core.Models.Revenue;

public record CustomerRevenueShare
{
    public required Rank Rank { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required FriendlyName OrganizationName { get; init; }
    public required RevenueAmount Revenue { get; init; }
    public required RevenueShare Share { get; init; }
}
