using Core.ValueObjects;

namespace Core.Models.Revenue;

public record ListCustomerHealthRequest
{
    public required DaysElapsed WindowDays { get; init; }
}
