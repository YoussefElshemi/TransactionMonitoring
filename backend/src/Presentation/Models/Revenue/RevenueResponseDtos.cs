namespace Presentation.Models.Revenue;

public record RecurringRevenueMonthResponseDto
{
    public required int Month { get; init; }
    public required int Year { get; init; }
    public required string Currency { get; init; }
    public required decimal OpeningRevenue { get; init; }
    public required decimal NewRevenue { get; init; }
    public required decimal ReactivationRevenue { get; init; }
    public required decimal ExpansionRevenue { get; init; }
    public required decimal ContractionRevenue { get; init; }
    public required decimal ChurnedRevenue { get; init; }
    public required decimal InvoluntaryChurnedRevenue { get; init; }
    public required decimal ClosingRevenue { get; init; }
    public required int OpeningCustomers { get; init; }
    public required int NewCustomers { get; init; }
    public required int ReactivatedCustomers { get; init; }
    public required int ChurnedCustomers { get; init; }
    public required int InvoluntarilyChurnedCustomers { get; init; }
    public required int ClosingCustomers { get; init; }
}

public record CustomerRevenueShareResponseDto
{
    public required int Rank { get; init; }
    public required Guid OrganizationId { get; init; }
    public required string OrganizationName { get; init; }
    public required decimal Revenue { get; init; }
    public required decimal Share { get; init; }
}

public record RevenueConcentrationResponseDto
{
    public required int Month { get; init; }
    public required int Year { get; init; }
    public required string Currency { get; init; }
    public required decimal TotalRevenue { get; init; }
    public required int CustomerCount { get; init; }

    public required decimal? LargestCustomerShare { get; init; }
    public required decimal? TopFiveShare { get; init; }
    public required decimal? TopTenShare { get; init; }
    public required decimal? ConcentrationIndex { get; init; }
    public required List<CustomerRevenueShareResponseDto> Customers { get; init; }
}

public record CustomerMarginLineResponseDto
{
    public required string BillableResource { get; init; }
    public required int Count { get; init; }

    public required decimal? UnitCost { get; init; }

    public required decimal Cost { get; init; }
}

public record CustomerMarginResponseDto
{
    public required Guid OrganizationId { get; init; }
    public required string OrganizationName { get; init; }
    public required int Month { get; init; }
    public required int Year { get; init; }
    public required string Currency { get; init; }
    public required decimal Revenue { get; init; }
    public required decimal Cost { get; init; }
    public required decimal Margin { get; init; }

    public required decimal? MarginRate { get; init; }

    public required bool CostsComplete { get; init; }

    public required List<CustomerMarginLineResponseDto> Lines { get; init; }
}

public record StageRecoveryResponseDto
{
    public required string Stage { get; init; }
    public required int Resolved { get; init; }
}

public record RecoveryStatisticResponseDto
{
    public required DateTimeOffset PeriodStart { get; init; }
    public required DateTimeOffset PeriodEnd { get; init; }
    public required string Currency { get; init; }
    public required int LaddersOpened { get; init; }
    public required int LaddersResolved { get; init; }

    public required decimal? RecoveryRate { get; init; }

    public required decimal RecoveredRevenue { get; init; }
    public required decimal OutstandingRevenue { get; init; }
    public required int RestrictionsProposed { get; init; }
    public required int? MeanDaysToRecover { get; init; }
    public required List<StageRecoveryResponseDto> ResolvedByStage { get; init; }
    public required int InvoluntarilyChurnedCustomers { get; init; }
    public required decimal? InvoluntaryChurnRate { get; init; }
}

public record HealthSignalResponseDto
{
    public required string Kind { get; init; }
    public required int Penalty { get; init; }
    public required string Detail { get; init; }
}

public record CustomerHealthResponseDto
{
    public required Guid OrganizationId { get; init; }
    public required string OrganizationName { get; init; }
    public required int HealthScore { get; init; }
    public required string HealthBand { get; init; }
    public required DateTimeOffset PeriodStart { get; init; }
    public required DateTimeOffset PeriodEnd { get; init; }
    public required int RecentUsage { get; init; }
    public required int PreviousUsage { get; init; }

    public required decimal? UsageTrend { get; init; }

    public required decimal RecentErrorRate { get; init; }
    public required decimal PreviousErrorRate { get; init; }
    public required int? DaysSinceLastUsage { get; init; }
    public required string? ArrearsStage { get; init; }

    public required List<HealthSignalResponseDto> Signals { get; init; }
}
