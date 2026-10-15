using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Revenue;

public record BilledMonth
{
    public required OrganizationId OrganizationId { get; init; }
    public required FriendlyName OrganizationName { get; init; }
    public required Month Month { get; init; }
    public required Year Year { get; init; }

    public required Currency Currency { get; init; }

    public required NetAmount NetAmount { get; init; }
    public required NetAmount CreditedNetAmount { get; init; }
}
