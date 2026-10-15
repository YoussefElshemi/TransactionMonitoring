using Core.ValueObjects;

namespace Core.Models.Revenue;

public record GetRecoveryStatisticRequest
{
    public required PeriodStart PeriodStart { get; init; }
    public required PeriodEnd PeriodEnd { get; init; }
}
