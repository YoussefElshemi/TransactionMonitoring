using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Revenue;

public record RevenueConcentration
{
    public required Month Month { get; init; }
    public required Year Year { get; init; }
    public required Currency Currency { get; init; }

    public required RevenueAmount TotalRevenue { get; init; }
    public required CustomerCount CustomerCount { get; init; }

    public required RevenueShare? LargestCustomerShare { get; init; }
    public required RevenueShare? TopFiveShare { get; init; }
    public required RevenueShare? TopTenShare { get; init; }
    public required ConcentrationIndex? ConcentrationIndex { get; init; }

    public required List<CustomerRevenueShare> Customers { get; init; }
}
