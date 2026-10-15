using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Revenue;

public record CustomerMargin
{
    public required OrganizationId OrganizationId { get; init; }
    public required FriendlyName OrganizationName { get; init; }
    public required Month Month { get; init; }
    public required Year Year { get; init; }
    public required Currency Currency { get; init; }

    public required RevenueAmount Revenue { get; init; }
    public required CostAmount Cost { get; init; }
    public required MarginAmount Margin { get; init; }

    public required MarginRate? MarginRate { get; init; }

    public required CostsComplete CostsComplete { get; init; }

    public required List<CustomerMarginLine> Lines { get; init; }
}
