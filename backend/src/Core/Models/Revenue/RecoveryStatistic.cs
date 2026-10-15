using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Revenue;

public record RecoveryStatistic
{
    public required PeriodStart PeriodStart { get; init; }
    public required PeriodEnd PeriodEnd { get; init; }
    public required Currency Currency { get; init; }

    public required Count LaddersOpened { get; init; }
    public required Count LaddersResolved { get; init; }

    public required RecoveryRate? RecoveryRate { get; init; }

    public required RevenueAmount RecoveredRevenue { get; init; }
    public required RevenueAmount OutstandingRevenue { get; init; }

    public required Count RestrictionsProposed { get; init; }

    public required DaysElapsed? MeanDaysToRecover { get; init; }

    public required List<StageRecovery> ResolvedByStage { get; init; }

    public required Count InvoluntarilyChurnedCustomers { get; init; }

    public required ChurnRate? InvoluntaryChurnRate { get; init; }
}
