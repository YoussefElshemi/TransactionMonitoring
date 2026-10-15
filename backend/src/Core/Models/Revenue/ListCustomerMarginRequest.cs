using Core.ValueObjects;

namespace Core.Models.Revenue;

public record ListCustomerMarginRequest
{
    public required Month Month { get; init; }
    public required Year Year { get; init; }
}
