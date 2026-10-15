using Core.Enums;
using Core.Exceptions;
using Core.Localization;
using Core.Models.Invoices;
using Core.Models.Plans;
using Core.ValueObjects;

namespace Core.Helpers;

public static class PricingCalculator
{
    // Each line is rounded as written so the total is exactly the sum of the printed lines.
    private const int MoneyDecimals = 2;

    public static List<InvoiceLine> Calculate(PriceInvoiceRequest priceInvoiceRequest, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        var lines = new List<InvoiceLine>();

        AddUsageLines(priceInvoiceRequest, lines, now);
        AddPlatformFeeLine(priceInvoiceRequest, lines, now);
        AddMinimumTopUpLine(priceInvoiceRequest, lines, now);
        AddDiscountLines(priceInvoiceRequest, lines, now);
        AddCommitmentDrawdownLine(priceInvoiceRequest, lines, now);
        AddAdjustmentLines(priceInvoiceRequest, lines, now);
        AddTaxLine(priceInvoiceRequest, lines, now);

        return lines;
    }

    public static InvoiceAmount Total(IEnumerable<InvoiceLine> lines)
    {
        return new InvoiceAmount(lines.Sum(x => x.Amount));
    }

    public static NetAmount Net(IEnumerable<InvoiceLine> lines)
    {
        return new NetAmount(lines.Where(x => x.InvoiceLineType != InvoiceLineType.Tax).Sum(x => x.Amount));
    }

    public static TaxAmount Tax(IEnumerable<InvoiceLine> lines)
    {
        return new TaxAmount(lines.Where(x => x.InvoiceLineType == InvoiceLineType.Tax).Sum(x => x.Amount));
    }

    private static void AddUsageLines(PriceInvoiceRequest priceInvoiceRequest, List<InvoiceLine> lines, DateTimeOffset now)
    {
        var quantities = priceInvoiceRequest.Quantities
            .Where(x => x.Value > 0)
            .OrderBy(x => (int)x.Key);

        foreach (var (billableResource, quantity) in quantities)
        {
            var planPrice = FindPrice(priceInvoiceRequest.PlanVersion, billableResource);

            var translator = priceInvoiceRequest.Translator;

            if (planPrice is null)
            {
                lines.Add(CreateLine(
                    priceInvoiceRequest.InvoiceId, InvoiceLineType.Usage, billableResource,
                    translator.Text(TranslationKeys.LineNotPriced, translator.BillableResource(billableResource)),
                    quantity, 0m, 0m, lines.Count, now));

                continue;
            }

            AddAllowanceLine(priceInvoiceRequest.InvoiceId, billableResource, planPrice, quantity, translator, lines, now);

            var chargeable = Math.Max(0, quantity - planPrice.FreeAllowance);

            if (chargeable == 0)
            {
                continue;
            }

            AddBandLines(priceInvoiceRequest.InvoiceId, billableResource, planPrice, chargeable, priceInvoiceRequest.ConversionRate, translator, lines, now);
        }
    }

    private static void AddAllowanceLine(
        InvoiceId invoiceId,
        BillableResource billableResource,
        PlanPrice planPrice,
        Count quantity,
        Translator translator,
        List<InvoiceLine> lines,
        DateTimeOffset now)
    {
        if (planPrice.FreeAllowance == 0)
        {
            return;
        }

        var consumed = Math.Min(quantity, planPrice.FreeAllowance);

        lines.Add(CreateLine(
            invoiceId, InvoiceLineType.Usage, billableResource,
            translator.Text(TranslationKeys.LineIncludedAllowance, translator.BillableResource(billableResource)),
            consumed, 0m, 0m, lines.Count, now));
    }

    private static void AddBandLines(
        InvoiceId invoiceId,
        BillableResource billableResource,
        PlanPrice planPrice,
        int chargeable,
        ConversionRate conversionRate,
        Translator translator,
        List<InvoiceLine> lines,
        DateTimeOffset now)
    {
        var tiers = OrderedTiers(planPrice);

        switch (planPrice.PricingModel)
        {
            case PricingModel.Flat:
                AddPricedLine(invoiceId, billableResource, translator.BillableResource(billableResource), chargeable, tiers[0], conversionRate, lines, now);
                break;

            case PricingModel.Graduated:
                AddGraduatedLines(invoiceId, billableResource, tiers, chargeable, conversionRate, translator, lines, now);
                break;

            case PricingModel.Volume:
                AddPricedLine(invoiceId, billableResource, translator.BillableResource(billableResource), chargeable, BandContaining(tiers, chargeable), conversionRate, lines, now);
                break;

            default:
                throw new InvalidPriceStructureException("'" + planPrice.PricingModel + "' is not a pricing model this can bill.");
        }
    }

    private static void AddGraduatedLines(
        InvoiceId invoiceId,
        BillableResource billableResource,
        List<PlanPriceTier> tiers,
        int chargeable,
        ConversionRate conversionRate,
        Translator translator,
        List<InvoiceLine> lines,
        DateTimeOffset now)
    {
        foreach (var tier in tiers)
        {
            var unitsInBand = UnitsInBand(tier, chargeable);

            if (unitsInBand == 0)
            {
                continue;
            }

            var description = tier.ToQuantity.HasValue
                ? translator.Text(TranslationKeys.LineBandRange,
                    translator.BillableResource(billableResource), tier.FromQuantity + 1, (int)tier.ToQuantity.Value)
                : translator.Text(TranslationKeys.LineBandAndAbove,
                    translator.BillableResource(billableResource), tier.FromQuantity + 1);

            AddPricedLine(invoiceId, billableResource, description, unitsInBand, tier, conversionRate, lines, now);
        }
    }

    private static void AddPricedLine(
        InvoiceId invoiceId,
        BillableResource billableResource,
        string description,
        int units,
        PlanPriceTier tier,
        ConversionRate conversionRate,
        List<InvoiceLine> lines,
        DateTimeOffset now)
    {
        // Round the line amount once, not the unit price, or the rounding error is multiplied across units.
        var unitAmount = tier.UnitAmount * conversionRate;
        var amount = units * unitAmount + tier.FlatAmount * conversionRate;

        lines.Add(CreateLine(
            invoiceId, InvoiceLineType.Usage, billableResource, description,
            units, unitAmount, amount, lines.Count, now));
    }

    private static void AddPlatformFeeLine(PriceInvoiceRequest priceInvoiceRequest, List<InvoiceLine> lines, DateTimeOffset now)
    {
        var platformFee = priceInvoiceRequest.PlanVersion.PlatformFee * priceInvoiceRequest.ConversionRate;

        if (platformFee == 0)
        {
            return;
        }

        lines.Add(CreateLine(
            priceInvoiceRequest.InvoiceId, InvoiceLineType.PlatformFee, null,
            priceInvoiceRequest.Translator.Text(TranslationKeys.LinePlatformFee),
            1, platformFee, platformFee, lines.Count, now));
    }

    private static void AddMinimumTopUpLine(PriceInvoiceRequest priceInvoiceRequest, List<InvoiceLine> lines, DateTimeOffset now)
    {
        var minimumCharge = Round(priceInvoiceRequest.PlanVersion.MinimumCharge * priceInvoiceRequest.ConversionRate);

        if (minimumCharge == 0)
        {
            return;
        }

        var shortfall = minimumCharge - Total(lines);

        if (shortfall <= 0)
        {
            return;
        }

        lines.Add(CreateLine(
            priceInvoiceRequest.InvoiceId, InvoiceLineType.MinimumTopUp, null,
            priceInvoiceRequest.Translator.Text(
                TranslationKeys.LineMinimumTopUp, Money(priceInvoiceRequest.Currency, minimumCharge)),
            1, shortfall, shortfall, lines.Count, now));
    }

    private static void AddDiscountLines(PriceInvoiceRequest priceInvoiceRequest, List<InvoiceLine> lines, DateTimeOffset now)
    {
        var discounts = priceInvoiceRequest.Discounts
            .OrderBy(x => (DateTimeOffset)x.EffectiveFrom)
            .ThenBy(x => (DateTimeOffset)x.CreatedAt);

        foreach (var discount in discounts)
        {
            var running = Total(lines);

            if (running <= 0)
            {
                continue;
            }

            var reduction = discount.DiscountType switch
            {
                DiscountType.Percentage => Round(running * discount.Value / 100m),

                DiscountType.FixedAmount => Math.Min(discount.Value, running),
                _ => throw new InvalidPriceStructureException("'" + discount.DiscountType + "' is not a discount this can apply.")
            };

            if (reduction <= 0)
            {
                continue;
            }

            lines.Add(CreateLine(
                priceInvoiceRequest.InvoiceId, InvoiceLineType.Discount, null,
                discount.Description, 1, reduction, -reduction, lines.Count, now));
        }
    }

    private static void AddCommitmentDrawdownLine(PriceInvoiceRequest priceInvoiceRequest, List<InvoiceLine> lines, DateTimeOffset now)
    {
        if (priceInvoiceRequest.Commitment is null)
        {
            return;
        }

        var payable = Total(lines);

        if (payable <= 0)
        {
            return;
        }

        var remaining = priceInvoiceRequest.Commitment.Amount - priceInvoiceRequest.Commitment.Drawn;
        var drawn = Math.Min(remaining, payable);

        if (drawn <= 0)
        {
            return;
        }

        var left = remaining - drawn;

        lines.Add(CreateLine(
            priceInvoiceRequest.InvoiceId, InvoiceLineType.CommitmentDrawdown, null,
            priceInvoiceRequest.Translator.Text(
                TranslationKeys.LineCommitmentDrawdown,
                Money(priceInvoiceRequest.Currency, left),
                Money(priceInvoiceRequest.Currency, priceInvoiceRequest.Commitment.Amount)),
            1, drawn, -drawn, lines.Count, now));
    }

    private static void AddAdjustmentLines(PriceInvoiceRequest priceInvoiceRequest, List<InvoiceLine> lines, DateTimeOffset now)
    {
        var adjustments = priceInvoiceRequest.Adjustments
            .OrderBy(x => (DateTimeOffset)x.CreatedAt);

        foreach (var adjustment in adjustments)
        {
            lines.Add(CreateLine(
                priceInvoiceRequest.InvoiceId, InvoiceLineType.Adjustment, null,
                adjustment.Description, 1, Math.Abs(adjustment.Amount), adjustment.Amount, lines.Count, now,
                adjustment.InvoiceAdjustmentId));
        }
    }

    private static void AddTaxLine(PriceInvoiceRequest priceInvoiceRequest, List<InvoiceLine> lines, DateTimeOffset now)
    {
        if (lines.Count == 0)
        {
            return;
        }

        var determination = priceInvoiceRequest.TaxDetermination;
        var net = Total(lines);
        var tax = Round(net * determination.TaxRate / 100m);

        lines.Add(CreateLine(
            priceInvoiceRequest.InvoiceId, InvoiceLineType.Tax, null,
            determination.Description, 1, tax, tax, lines.Count, now));
    }

    private static string Money(Currency currency, decimal amount)
    {
        return CurrencyFormatter.Format(currency, amount);
    }

    private static PlanPrice? FindPrice(PlanVersion planVersion, BillableResource billableResource)
    {
        return planVersion.Prices.Find(x => x.BillableResource == billableResource)
               ?? planVersion.Prices.Find(x => x.BillableResource is null);
    }

    private static List<PlanPriceTier> OrderedTiers(PlanPrice planPrice)
    {
        var tiers = planPrice.Tiers.OrderBy(x => (int)x.FromQuantity).ToList();

        if (tiers.Count == 0)
        {
            throw new InvalidPriceStructureException("the price for '" + Describe(planPrice.BillableResource) + "' has no bands.");
        }

        return tiers;
    }

    // Bands are half-open over unit positions: [FromQuantity, ToQuantity).
    private static int UnitsInBand(PlanPriceTier tier, int chargeable)
    {
        var upperEdge = tier.ToQuantity.HasValue ? Math.Min(chargeable, tier.ToQuantity.Value) : chargeable;

        return Math.Max(0, upperEdge - tier.FromQuantity);
    }

    private static PlanPriceTier BandContaining(List<PlanPriceTier> tiers, int chargeable)
    {
        var lastUnit = chargeable - 1;

        var tier = tiers.Find(x =>
            x.FromQuantity <= lastUnit &&
            (!x.ToQuantity.HasValue || lastUnit < x.ToQuantity.Value));

        return tier ?? throw new InvalidPriceStructureException("no band covers a quantity of " + chargeable + ".");
    }

    private static InvoiceLine CreateLine(
        InvoiceId invoiceId,
        InvoiceLineType invoiceLineType,
        BillableResource? billableResource,
        string description,
        int quantity,
        decimal unitAmount,
        decimal amount,
        int sortOrder,
        DateTimeOffset now,
        InvoiceAdjustmentId? invoiceAdjustmentId = null)
    {
        return new InvoiceLine
        {
            InvoiceLineId = new InvoiceLineId(Guid.NewGuid()),
            InvoiceId = invoiceId,
            InvoiceLineType = invoiceLineType,
            BillableResource = billableResource,
            Description = new InvoiceLineDescription(description),
            Quantity = new Quantity(quantity),
            UnitAmount = new UnitAmount(unitAmount),
            Amount = new LineAmount(Round(amount)),
            InvoiceAdjustmentId = invoiceAdjustmentId,
            SortOrder = new SortOrder(sortOrder),
            CreatedAt = new CreatedAt(now)
        };
    }

    private static decimal Round(decimal value)
    {
        return Math.Round(value, MoneyDecimals, MidpointRounding.AwayFromZero);
    }

    private static string Describe(BillableResource? billableResource)
    {
        return billableResource switch
        {
            BillableResource.RuleEvaluation => "Rule evaluations",
            BillableResource.RuleSetEvaluation => "Rule set evaluations",
            BillableResource.RuleTest => "Rule tests",
            BillableResource.Backtest => "Backtested evaluations",
            BillableResource.SandboxWrite => "Sandbox writes",
            _ => "Metered usage"
        };
    }
}
