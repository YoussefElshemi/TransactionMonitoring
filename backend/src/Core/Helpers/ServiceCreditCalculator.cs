using Core.Models.Availability;
using Core.ValueObjects;

namespace Core.Helpers;

public static class ServiceCreditCalculator
{
    public static CreditPercentage Resolve(IReadOnlyList<ServiceCreditBand> bands, UptimePercentage measured)
    {
        var applicable = bands
            .Where(x => measured < x.BelowUptimePercentage)
            .Select(x => (decimal)x.CreditPercentage)
            .ToList();

        return new CreditPercentage(applicable.Count == 0 ? 0m : applicable.Max());
    }

    public static ServiceCreditAmount Calculate(CreditPercentage creditPercentage, NetAmount invoicedNetAmount)
    {
        var amount = invoicedNetAmount * creditPercentage / 100m;

        return new ServiceCreditAmount(Math.Round(Math.Max(amount, 0m), 2, MidpointRounding.AwayFromZero));
    }
}
