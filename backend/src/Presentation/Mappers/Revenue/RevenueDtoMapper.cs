using Core.Helpers;
using Core.Models.Revenue;
using Core.ValueObjects;
using Presentation.Models.Revenue;

namespace Presentation.Mappers.Revenue;

public static class RevenueDtoMapper
{
    public static ListRecurringRevenueRequest ToModel(this ListRecurringRevenueRequestDto dto)
    {
        return new ListRecurringRevenueRequest
        {
            FromMonth = new Month(dto.FromMonth),
            FromYear = new Year(dto.FromYear),
            ToMonth = new Month(dto.ToMonth),
            ToYear = new Year(dto.ToYear)
        };
    }

    public static GetRevenueConcentrationRequest ToModel(this GetRevenueConcentrationRequestDto dto)
    {
        return new GetRevenueConcentrationRequest
        {
            Month = new Month(dto.Month),
            Year = new Year(dto.Year),
            TopCount = new Count(dto.TopCount)
        };
    }

    public static ListCustomerMarginRequest ToModel(this ListCustomerMarginRequestDto dto)
    {
        return new ListCustomerMarginRequest
        {
            Month = new Month(dto.Month),
            Year = new Year(dto.Year)
        };
    }

    public static GetRecoveryStatisticRequest ToModel(this GetRecoveryStatisticRequestDto dto)
    {
        return new GetRecoveryStatisticRequest
        {
            PeriodStart = new PeriodStart(dto.PeriodStart),
            PeriodEnd = new PeriodEnd(dto.PeriodEnd)
        };
    }

    public static ListCustomerHealthRequest ToModel(this ListCustomerHealthRequestDto dto)
    {
        return new ListCustomerHealthRequest
        {
            WindowDays = new DaysElapsed(dto.WindowDays)
        };
    }

    public static ExportRevenueRecognitionRequest ToModel(this ExportRevenueRecognitionRequestDto dto)
    {
        return new ExportRevenueRecognitionRequest
        {
            FromMonth = new Month(dto.FromMonth),
            FromYear = new Year(dto.FromYear),
            ToMonth = new Month(dto.ToMonth),
            ToYear = new Year(dto.ToYear)
        };
    }

    public static RecurringRevenueMonthResponseDto ToDto(this RecurringRevenueMonth month)
    {
        return new RecurringRevenueMonthResponseDto
        {
            Month = month.Month,
            Year = month.Year,
            Currency = CurrencyFormatter.Code(month.Currency),
            OpeningRevenue = month.OpeningRevenue,
            NewRevenue = month.NewRevenue,
            ReactivationRevenue = month.ReactivationRevenue,
            ExpansionRevenue = month.ExpansionRevenue,
            ContractionRevenue = month.ContractionRevenue,
            ChurnedRevenue = month.ChurnedRevenue,
            InvoluntaryChurnedRevenue = month.InvoluntaryChurnedRevenue,
            ClosingRevenue = month.ClosingRevenue,
            OpeningCustomers = month.OpeningCustomers,
            NewCustomers = month.NewCustomers,
            ReactivatedCustomers = month.ReactivatedCustomers,
            ChurnedCustomers = month.ChurnedCustomers,
            InvoluntarilyChurnedCustomers = month.InvoluntarilyChurnedCustomers,
            ClosingCustomers = month.ClosingCustomers
        };
    }

    public static RevenueConcentrationResponseDto ToDto(this RevenueConcentration concentration)
    {
        return new RevenueConcentrationResponseDto
        {
            Month = concentration.Month,
            Year = concentration.Year,
            Currency = CurrencyFormatter.Code(concentration.Currency),
            TotalRevenue = concentration.TotalRevenue,
            CustomerCount = concentration.CustomerCount,
            LargestCustomerShare = concentration.LargestCustomerShare.HasValue ? concentration.LargestCustomerShare.Value : null,
            TopFiveShare = concentration.TopFiveShare.HasValue ? concentration.TopFiveShare.Value : null,
            TopTenShare = concentration.TopTenShare.HasValue ? concentration.TopTenShare.Value : null,
            ConcentrationIndex = concentration.ConcentrationIndex.HasValue ? concentration.ConcentrationIndex.Value : null,
            Customers = concentration.Customers.ConvertAll(x => x.ToDto())
        };
    }

    public static CustomerRevenueShareResponseDto ToDto(this CustomerRevenueShare share)
    {
        return new CustomerRevenueShareResponseDto
        {
            Rank = share.Rank,
            OrganizationId = share.OrganizationId,
            OrganizationName = share.OrganizationName,
            Revenue = share.Revenue,
            Share = share.Share
        };
    }

    public static CustomerMarginResponseDto ToDto(this CustomerMargin margin)
    {
        return new CustomerMarginResponseDto
        {
            OrganizationId = margin.OrganizationId,
            OrganizationName = margin.OrganizationName,
            Month = margin.Month,
            Year = margin.Year,
            Currency = CurrencyFormatter.Code(margin.Currency),
            Revenue = margin.Revenue,
            Cost = margin.Cost,
            Margin = margin.Margin,
            MarginRate = margin.MarginRate.HasValue ? margin.MarginRate.Value : null,
            CostsComplete = margin.CostsComplete,
            Lines = margin.Lines.ConvertAll(x => x.ToDto())
        };
    }

    public static CustomerMarginLineResponseDto ToDto(this CustomerMarginLine line)
    {
        return new CustomerMarginLineResponseDto
        {
            BillableResource = line.BillableResource.ToString(),
            Count = line.Count,
            UnitCost = line.UnitCost.HasValue ? line.UnitCost.Value : null,
            Cost = line.Cost
        };
    }

    public static RecoveryStatisticResponseDto ToDto(this RecoveryStatistic recovery)
    {
        return new RecoveryStatisticResponseDto
        {
            PeriodStart = recovery.PeriodStart,
            PeriodEnd = recovery.PeriodEnd,
            Currency = CurrencyFormatter.Code(recovery.Currency),
            LaddersOpened = recovery.LaddersOpened,
            LaddersResolved = recovery.LaddersResolved,
            RecoveryRate = recovery.RecoveryRate.HasValue ? recovery.RecoveryRate.Value : null,
            RecoveredRevenue = recovery.RecoveredRevenue,
            OutstandingRevenue = recovery.OutstandingRevenue,
            RestrictionsProposed = recovery.RestrictionsProposed,
            MeanDaysToRecover = recovery.MeanDaysToRecover.HasValue ? recovery.MeanDaysToRecover.Value : null,
            ResolvedByStage = recovery.ResolvedByStage.ConvertAll(x => new StageRecoveryResponseDto
            {
                Stage = x.Stage.ToString(),
                Resolved = x.Resolved
            }),
            InvoluntarilyChurnedCustomers = recovery.InvoluntarilyChurnedCustomers,
            InvoluntaryChurnRate = recovery.InvoluntaryChurnRate.HasValue ? recovery.InvoluntaryChurnRate.Value : null
        };
    }

    public static CustomerHealthResponseDto ToDto(this CustomerHealth health)
    {
        return new CustomerHealthResponseDto
        {
            OrganizationId = health.OrganizationId,
            OrganizationName = health.OrganizationName,
            HealthScore = health.HealthScore,
            HealthBand = health.HealthBand.ToString(),
            PeriodStart = health.PeriodStart,
            PeriodEnd = health.PeriodEnd,
            RecentUsage = health.RecentUsage,
            PreviousUsage = health.PreviousUsage,
            UsageTrend = health.UsageTrend.HasValue ? health.UsageTrend.Value : null,
            RecentErrorRate = health.RecentErrorRate,
            PreviousErrorRate = health.PreviousErrorRate,
            DaysSinceLastUsage = health.DaysSinceLastUsage.HasValue ? health.DaysSinceLastUsage.Value : null,
            ArrearsStage = health.ArrearsStage?.ToString(),
            Signals = health.Signals.ConvertAll(x => new HealthSignalResponseDto
            {
                Kind = x.Kind.ToString(),
                Penalty = x.Penalty,
                Detail = x.Detail
            })
        };
    }
}
