using Core.ValueObjects;

namespace Core.Models.Revenue;

public record GetRevenueConcentrationRequest
{
    public required Month Month { get; init; }
    public required Year Year { get; init; }

    public required Count TopCount { get; init; }
}
