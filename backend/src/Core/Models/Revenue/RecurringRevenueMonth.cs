using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Revenue;

public record RecurringRevenueMonth
{
    public required Month Month { get; init; }
    public required Year Year { get; init; }

    public required Currency Currency { get; init; }

    public required RevenueAmount OpeningRevenue { get; init; }

    public required RevenueAmount NewRevenue { get; init; }
    public required RevenueAmount ReactivationRevenue { get; init; }

    public required RevenueAmount ExpansionRevenue { get; init; }
    public required RevenueAmount ContractionRevenue { get; init; }

    public required RevenueAmount ChurnedRevenue { get; init; }
    public required RevenueAmount InvoluntaryChurnedRevenue { get; init; }

    public required RevenueAmount ClosingRevenue { get; init; }

    public required CustomerCount OpeningCustomers { get; init; }
    public required CustomerCount NewCustomers { get; init; }
    public required CustomerCount ReactivatedCustomers { get; init; }
    public required CustomerCount ChurnedCustomers { get; init; }
    public required CustomerCount InvoluntarilyChurnedCustomers { get; init; }
    public required CustomerCount ClosingCustomers { get; init; }
}
