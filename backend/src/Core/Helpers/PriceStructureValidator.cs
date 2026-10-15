using Core.Enums;
using Core.Exceptions;
using Core.Models.Plans;

namespace Core.Helpers;

public static class PriceStructureValidator
{
    public static void Validate(List<CreatePlanPriceRequest> prices)
    {
        var named = prices.Where(x => x.BillableResource.HasValue).ToList();

        var duplicated = named
            .GroupBy(x => x.BillableResource!.Value)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicated is not null)
        {
            throw new InvalidPriceStructureException($"'{duplicated.Key}' is priced twice. A version prices each kind of work once.");
        }

        if (prices.Count(x => x.BillableResource is null) > 1)
        {
            throw new InvalidPriceStructureException("there is more than one catch-all price. A version has at most one.");
        }

        foreach (var price in prices)
        {
            ValidateTiers(price);
        }
    }

    private static void ValidateTiers(CreatePlanPriceRequest price)
    {
        var name = price.BillableResource?.ToString() ?? "the catch-all price";
        var tiers = price.Tiers.OrderBy(x => (int)x.FromQuantity).ToList();

        if (tiers.Count == 0)
        {
            throw new InvalidPriceStructureException($"{name} has no bands.");
        }

        if (tiers[0].FromQuantity != 0)
        {
            throw new InvalidPriceStructureException($"{name} does not start at zero, so the first units it meters are unpriced.");
        }

        for (var index = 0; index < tiers.Count - 1; index++)
        {
            var tier = tiers[index];

            if (!tier.ToQuantity.HasValue)
            {
                throw new InvalidPriceStructureException($"{name} has an unbounded band below its top one, so the bands above it are unreachable.");
            }

            if (tier.ToQuantity.Value != tiers[index + 1].FromQuantity)
            {
                throw new InvalidPriceStructureException(
                    $"{name} has a break between {tier.ToQuantity.Value} and {tiers[index + 1].FromQuantity}, so quantities in between are unpriced.");
            }
        }

        if (tiers[^1].ToQuantity.HasValue)
        {
            throw new InvalidPriceStructureException($"{name} has a bounded top band, so any quantity above {tiers[^1].ToQuantity!.Value} is unpriced.");
        }

        if (price.PricingModel == PricingModel.Flat && tiers.Count > 1)
        {
            throw new InvalidPriceStructureException(
                $"{name} is flat but has {tiers.Count} bands. A flat price charges one rate, so bands beyond the first would never be charged.");
        }
    }
}
