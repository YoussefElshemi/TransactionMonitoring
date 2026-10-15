using Core.Configs;
using Core.Enums;

namespace Core.Helpers;

public static class DunningLadder
{
    public static (DunningStage Stage, DateTimeOffset NextAttemptAt) Open(DateTimeOffset dueAt, PaymentConfig paymentConfig)
    {
        return (DunningStage.Grace, dueAt.AddDays(paymentConfig.GraceDays));
    }

    public static DunningStage? Next(DunningStage stage)
    {
        return stage switch
        {
            DunningStage.Grace => DunningStage.FirstNotice,
            DunningStage.FirstNotice => DunningStage.SecondNotice,
            DunningStage.SecondNotice => DunningStage.FinalNotice,
            DunningStage.FinalNotice => DunningStage.RestrictionProposed,
            _ => null
        };
    }

    public static DateTimeOffset NextAttemptAt(DunningStage stage, DateTimeOffset asOf, PaymentConfig paymentConfig)
    {
        var intervals = paymentConfig.NoticeIntervalDays;

        return stage switch
        {
            DunningStage.FirstNotice => asOf.AddDays(IntervalBetween(intervals, 0, 1)),
            DunningStage.SecondNotice => asOf.AddDays(IntervalBetween(intervals, 1, 2)),
            DunningStage.FinalNotice => asOf.AddDays(paymentConfig.RestrictionProposalDays),

            _ => asOf
        };
    }

    public static bool Collects(DunningStage stage)
    {
        return stage is DunningStage.FirstNotice or DunningStage.SecondNotice or DunningStage.FinalNotice;
    }

    private static int IntervalBetween(int[] intervals, int from, int to)
    {
        var earlier = from < intervals.Length ? intervals[from] : 0;
        var later = to < intervals.Length ? intervals[to] : earlier + 7;

        return Math.Max(later - earlier, 1);
    }
}
