using Core.Configs;
using Core.Enums;
using Core.Models.Revenue;
using Core.ValueObjects;

namespace Core.Helpers;

public static class CustomerHealthScorer
{
    private const int UsageDeclinePenalty = 20;
    private const int SevereUsageDeclinePenalty = 40;
    private const int ErrorRateRisePenalty = 15;
    private const int SilencePenalty = 30;

    private const int ArrearsPenalty = 15;
    private const int EscalatedArrearsPenalty = 30;

    public static (HealthScore Score, HealthBand Band, List<HealthSignal> Signals) Score(
        TrendRate? usageTrend,
        ErrorRate recentErrorRate,
        ErrorRate previousErrorRate,
        DaysElapsed? daysSinceLastUsage,
        DunningStage? arrearsStage,
        RevenueConfig revenueConfig)
    {
        var signals = new List<HealthSignal>();

        AddUsageSignal(signals, usageTrend, revenueConfig);
        AddErrorSignal(signals, recentErrorRate, previousErrorRate, revenueConfig);
        AddSilenceSignal(signals, daysSinceLastUsage, revenueConfig);
        AddArrearsSignal(signals, arrearsStage);

        var deducted = signals.Sum(x => x.Penalty);
        var score = new HealthScore(Math.Max(0, 100 - deducted));

        var band = score >= revenueConfig.HealthyScore
            ? HealthBand.Healthy
            : score >= revenueConfig.WatchScore
                ? HealthBand.Watch
                : HealthBand.AtRisk;

        return (score, band, signals);
    }

    private static void AddUsageSignal(List<HealthSignal> signals, TrendRate? usageTrend, RevenueConfig revenueConfig)
    {
        if (!usageTrend.HasValue)
        {
            signals.Add(new HealthSignal
            {
                Kind = HealthSignalKind.NoBaseline,
                Penalty = new ScorePenalty(0),
                Detail = new SignalDetail("No traffic in the earlier window to compare against")
            });

            return;
        }

        var trend = (decimal)usageTrend.Value;

        if (trend > revenueConfig.UsageDeclineThreshold)
        {
            return;
        }

        var severe = trend <= revenueConfig.SevereUsageDeclineThreshold;

        signals.Add(new HealthSignal
        {
            Kind = HealthSignalKind.UsageDecline,
            Penalty = new ScorePenalty(severe ? SevereUsageDeclinePenalty : UsageDeclinePenalty),
            Detail = new SignalDetail($"Metered work fell {Math.Abs(trend):F1}% against the previous window")
        });
    }

    private static void AddErrorSignal(
        List<HealthSignal> signals, ErrorRate recentErrorRate, ErrorRate previousErrorRate, RevenueConfig revenueConfig)
    {
        var rise = recentErrorRate - previousErrorRate;

        if (rise < revenueConfig.ErrorRateRiseThreshold)
        {
            return;
        }

        signals.Add(new HealthSignal
        {
            Kind = HealthSignalKind.ErrorRateRise,
            Penalty = new ScorePenalty(ErrorRateRisePenalty),
            Detail = new SignalDetail(
                $"We failed {(decimal)recentErrorRate:F2}% of their requests, against {(decimal)previousErrorRate:F2}% before")
        });
    }

    private static void AddSilenceSignal(List<HealthSignal> signals, DaysElapsed? daysSinceLastUsage, RevenueConfig revenueConfig)
    {
        if (!daysSinceLastUsage.HasValue || daysSinceLastUsage.Value < revenueConfig.SilenceDays)
        {
            return;
        }

        signals.Add(new HealthSignal
        {
            Kind = HealthSignalKind.Silence,
            Penalty = new ScorePenalty(SilencePenalty),
            Detail = new SignalDetail($"Nothing metered for {(int)daysSinceLastUsage.Value} days")
        });
    }

    private static void AddArrearsSignal(List<HealthSignal> signals, DunningStage? arrearsStage)
    {
        if (!arrearsStage.HasValue)
        {
            return;
        }

        var escalated = arrearsStage.Value is DunningStage.FinalNotice or DunningStage.RestrictionProposed;

        signals.Add(new HealthSignal
        {
            Kind = HealthSignalKind.Arrears,
            Penalty = new ScorePenalty(escalated ? EscalatedArrearsPenalty : ArrearsPenalty),
            Detail = new SignalDetail($"Being chased for a late invoice, at {arrearsStage.Value}")
        });
    }
}
