using Core.Enums;
using Core.Models.Revenue;
using Core.ValueObjects;

namespace Core.Helpers;

public static class RevenueMovementCalculator
{
    private const int ArrearsMonthsBefore = 1;
    private const int ArrearsMonthsAfter = 3;

    public static List<RecurringRevenueMonth> Calculate(
        IReadOnlyDictionary<int, IReadOnlyDictionary<Guid, decimal>> revenueByMonthKey,
        IReadOnlyDictionary<Guid, int> firstBilledMonthKey,
        IReadOnlyDictionary<Guid, IReadOnlySet<int>> escalatedArrearsMonthKeys,
        Currency currency,
        int firstMonthKey,
        int lastMonthKey)
    {
        var months = new List<RecurringRevenueMonth>();

        for (var monthKey = firstMonthKey; monthKey <= lastMonthKey; monthKey++)
        {
            var previous = revenueByMonthKey.GetValueOrDefault(monthKey - 1) ?? new Dictionary<Guid, decimal>();
            var current = revenueByMonthKey.GetValueOrDefault(monthKey) ?? new Dictionary<Guid, decimal>();

            months.Add(CalculateMonth(
                monthKey, previous, current, firstBilledMonthKey, escalatedArrearsMonthKeys, currency));
        }

        return months;
    }

    private static RecurringRevenueMonth CalculateMonth(
        int monthKey,
        IReadOnlyDictionary<Guid, decimal> previous,
        IReadOnlyDictionary<Guid, decimal> current,
        IReadOnlyDictionary<Guid, int> firstBilledMonthKey,
        IReadOnlyDictionary<Guid, IReadOnlySet<int>> escalatedArrearsMonthKeys,
        Currency currency)
    {
        decimal newRevenue = 0, reactivation = 0, expansion = 0, contraction = 0, churn = 0, involuntaryChurn = 0;
        int newCustomers = 0, reactivated = 0, churned = 0, involuntarilyChurned = 0;

        foreach (var organizationId in previous.Keys.Union(current.Keys))
        {
            var before = previous.GetValueOrDefault(organizationId);
            var after = current.GetValueOrDefault(organizationId);

            var billedBefore = before != 0;
            var billsNow = after != 0;

            switch (billedBefore, billsNow)
            {
                case (false, true) when firstBilledMonthKey.GetValueOrDefault(organizationId, monthKey) >= monthKey:
                    newRevenue += after;
                    newCustomers++;
                    break;

                case (false, true):
                    reactivation += after;
                    reactivated++;
                    break;

                case (true, false):
                    churn += before;
                    churned++;

                    if (WasInArrears(organizationId, monthKey, escalatedArrearsMonthKeys))
                    {
                        involuntaryChurn += before;
                        involuntarilyChurned++;
                    }

                    break;

                case (true, true) when after > before:
                    expansion += after - before;
                    break;

                case (true, true) when after < before:
                    contraction += before - after;
                    break;
            }
        }

        var (month, year) = BillingPeriod.FromKey(monthKey);

        return new RecurringRevenueMonth
        {
            Month = month,
            Year = year,
            Currency = currency,
            OpeningRevenue = new RevenueAmount(previous.Values.Sum()),
            NewRevenue = new RevenueAmount(newRevenue),
            ReactivationRevenue = new RevenueAmount(reactivation),
            ExpansionRevenue = new RevenueAmount(expansion),
            ContractionRevenue = new RevenueAmount(contraction),
            ChurnedRevenue = new RevenueAmount(churn),
            InvoluntaryChurnedRevenue = new RevenueAmount(involuntaryChurn),
            ClosingRevenue = new RevenueAmount(current.Values.Sum()),
            OpeningCustomers = new CustomerCount(previous.Count(x => x.Value != 0)),
            NewCustomers = new CustomerCount(newCustomers),
            ReactivatedCustomers = new CustomerCount(reactivated),
            ChurnedCustomers = new CustomerCount(churned),
            InvoluntarilyChurnedCustomers = new CustomerCount(involuntarilyChurned),
            ClosingCustomers = new CustomerCount(current.Count(x => x.Value != 0))
        };
    }

    private static bool WasInArrears(
        Guid organizationId,
        int monthKey,
        IReadOnlyDictionary<Guid, IReadOnlySet<int>> escalatedArrearsMonthKeys)
    {
        if (!escalatedArrearsMonthKeys.TryGetValue(organizationId, out var arrearsMonthKeys))
        {
            return false;
        }

        for (var key = monthKey - ArrearsMonthsBefore; key <= monthKey + ArrearsMonthsAfter; key++)
        {
            if (arrearsMonthKeys.Contains(key))
            {
                return true;
            }
        }

        return false;
    }
}
