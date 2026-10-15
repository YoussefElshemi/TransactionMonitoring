using Core.Enums;
using Core.Exceptions;
using Core.Helpers;
using Core.Models.Adjustments;
using Core.Models.Commitments;
using Core.Models.Discounts;
using Core.Models.Invoices;
using Core.Models.Plans;
using Core.Models.Tax;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Adjustments;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Commitments;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Discounts;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Localization;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Plans;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Tax;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Helpers;

public class PricingCalculatorTests : BaseTestClass
{
    private static PlanPriceTier Band(int from, int? to, decimal unit, decimal flat = 0)
    {
        return new PlanPriceTier
        {
            PlanPriceTierId = new PlanPriceTierId(Guid.NewGuid()),
            PlanPriceId = new PlanPriceId(Guid.Empty),
            FromQuantity = new FromQuantity(from),
            ToQuantity = to.HasValue ? new ToQuantity(to.Value) : null,
            UnitAmount = new UnitAmount(unit),
            FlatAmount = new FlatAmount(flat),
            CreatedAt = new CreatedAt(DateTimeOffset.UnixEpoch)
        };
    }

    private static PlanPrice Price(
        BillableResource? billableResource,
        PricingModel pricingModel,
        int freeAllowance,
        params PlanPriceTier[] tiers)
    {
        return new PlanPrice
        {
            PlanPriceId = new PlanPriceId(Guid.NewGuid()),
            PlanVersionId = new PlanVersionId(Guid.Empty),
            BillableResource = billableResource,
            PricingModel = pricingModel,
            FreeAllowance = new FreeAllowance(freeAllowance),
            Tiers = [..tiers],
            CreatedAt = new CreatedAt(DateTimeOffset.UnixEpoch)
        };
    }

    private PlanVersion Version(decimal platformFee, decimal minimumCharge, params PlanPrice[] prices)
    {
        return FakePlanVersion.CreateValid(Fixture) with
        {
            PlatformFee = new PlatformFee(platformFee),
            MinimumCharge = new MinimumCharge(minimumCharge),
            Prices = [..prices]
        };
    }

    private PriceInvoiceRequest Request(
        PlanVersion planVersion,
        Dictionary<BillableResource, Count>? quantities = null,
        List<ContractDiscount>? discounts = null,
        List<InvoiceAdjustment>? adjustments = null,
        Commitment? commitment = null,
        ConversionRate? conversionRate = null,
        TaxDetermination? taxDetermination = null)
    {
        return new PriceInvoiceRequest
        {
            InvoiceId = FakeInvoiceId.CreateValid(Fixture),
            PlanVersion = planVersion,
            Translator = FakeDisplayPreferences.Translator(),
            Quantities = quantities ?? [],
            Discounts = discounts ?? [],
            Adjustments = adjustments ?? [],
            Commitment = commitment,
            Currency = Currency.Gbp,
            ConversionRate = conversionRate ?? new ConversionRate(1m),

            TaxDetermination = taxDetermination ?? FakeTaxDetermination.CreateValid(Fixture)
        };
    }

    private List<InvoiceLine> Calculate(PriceInvoiceRequest request)
    {
        return PricingCalculator.Calculate(request, MockTimeProvider);
    }

    private static List<InvoiceLine> Priced(List<InvoiceLine> lines)
    {
        return lines.FindAll(x => x.InvoiceLineType != InvoiceLineType.Tax);
    }

    [Fact]
    public void Calculate_GivenAFlatPrice_ChargesEveryUnitAtTheSameRate()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Flat, 0, Band(0, null, 0.02m)));
        var request = Request(planVersion, new Dictionary<BillableResource, Count>
        {
            [BillableResource.RuleEvaluation] = new(10000)
        });

        // Act
        var lines = Calculate(request);

        // Assert
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(200m));
    }

    [Fact]
    public void Calculate_GivenGraduatedBands_PricesEachUnitInTheBandItFallsIn()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(
            null, PricingModel.Graduated, 0,
            Band(0, 1000, 0.10m),
            Band(1000, 5000, 0.05m),
            Band(5000, null, 0.02m)));

        var request = Request(planVersion, new Dictionary<BillableResource, Count>
        {
            [BillableResource.RuleEvaluation] = new(10000)
        });

        // Act
        var lines = Calculate(request);

        // Assert
        Priced(lines).Should().HaveCount(3);
        lines[0].Quantity.Should().Be(new Quantity(1000));
        lines[1].Quantity.Should().Be(new Quantity(4000));
        lines[2].Quantity.Should().Be(new Quantity(5000));

        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(100m + 200m + 100m));
    }

    [Fact]
    public void Calculate_GivenVolumeBands_PricesEveryUnitInTheBandTheTotalReaches()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(
            null, PricingModel.Volume, 0,
            Band(0, 1000, 0.10m),
            Band(1000, 5000, 0.05m),
            Band(5000, null, 0.02m)));

        var request = Request(planVersion, new Dictionary<BillableResource, Count>
        {
            [BillableResource.RuleEvaluation] = new(10000)
        });

        // Act
        var lines = Calculate(request);

        // Assert
        Priced(lines).Should().ContainSingle();
        lines[0].Quantity.Should().Be(new Quantity(10000));
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(200m));
    }

    [Fact]
    public void Calculate_GivenVolumeAndAQuantityOnABandEdge_UsesTheLowerBand()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(
            null, PricingModel.Volume, 0,
            Band(0, 1000, 0.10m),
            Band(1000, null, 0.01m)));

        var request = Request(planVersion, new Dictionary<BillableResource, Count>
        {
            [BillableResource.RuleEvaluation] = new(1000)
        });

        // Act
        var lines = Calculate(request);

        // Assert
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(100m));
    }

    [Fact]
    public void Calculate_GivenAFreeAllowance_TakesItOffTheQuantityBeforeTheBands()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(
            null, PricingModel.Graduated, 1000,
            Band(0, 1000, 0.10m),
            Band(1000, null, 0.01m)));

        var request = Request(planVersion, new Dictionary<BillableResource, Count>
        {
            [BillableResource.RuleEvaluation] = new(1200)
        });

        // Act
        var lines = Calculate(request);

        // Assert
        var allowance = Priced(lines).Single(x => x.Amount == 0);
        allowance.Quantity.Should().Be(new Quantity(1000));

        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(20m));
    }

    [Fact]
    public void Calculate_GivenUsageWithinTheAllowance_ChargesNothing()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Flat, 1000, Band(0, null, 0.10m)));
        var request = Request(planVersion, new Dictionary<BillableResource, Count>
        {
            [BillableResource.RuleEvaluation] = new(500)
        });

        // Act
        var lines = Calculate(request);

        // Assert
        Priced(lines).Should().ContainSingle();
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(0m));
    }

    [Fact]
    public void Calculate_GivenPerResourcePrices_ChargesEachKindOfWorkAtItsOwnRate()
    {
        // Arrange
        var planVersion = Version(
            0, 0,
            Price(null, PricingModel.Flat, 0, Band(0, null, 0.01m)),
            Price(BillableResource.Backtest, PricingModel.Flat, 0, Band(0, null, 0.001m)));

        var request = Request(planVersion, new Dictionary<BillableResource, Count>
        {
            [BillableResource.RuleEvaluation] = new(1000),
            [BillableResource.Backtest] = new(1000)
        });

        // Act
        var lines = Calculate(request);

        // Assert
        lines.Single(x => x.BillableResource == BillableResource.RuleEvaluation).Amount.Should().Be(new LineAmount(10m));
        lines.Single(x => x.BillableResource == BillableResource.Backtest).Amount.Should().Be(new LineAmount(1m));
    }

    [Fact]
    public void Calculate_GivenUsageNoPriceCovers_RecordsItAtZeroRatherThanOmittingIt()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(BillableResource.RuleEvaluation, PricingModel.Flat, 0, Band(0, null, 0.01m)));
        var request = Request(planVersion, new Dictionary<BillableResource, Count>
        {
            [BillableResource.Backtest] = new(500)
        });

        // Act
        var lines = Calculate(request);

        // Assert
        Priced(lines).Should().ContainSingle();
        lines[0].BillableResource.Should().Be(BillableResource.Backtest);
        lines[0].Quantity.Should().Be(new Quantity(500));
        lines[0].Amount.Should().Be(new LineAmount(0m));
    }

    [Fact]
    public void Calculate_GivenAPlatformFee_ChargesItWhetherOrNotAnythingWasUsed()
    {
        // Arrange
        var planVersion = Version(500m, 0, Price(null, PricingModel.Flat, 0, Band(0, null, 0.01m)));
        var request = Request(planVersion);

        // Act
        var lines = Calculate(request);

        // Assert
        Priced(lines).Should().ContainSingle();
        lines[0].InvoiceLineType.Should().Be(InvoiceLineType.PlatformFee);
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(500m));
    }

    [Fact]
    public void Calculate_GivenUsageBelowTheMinimum_AddsOnlyTheShortfall()
    {
        // Arrange
        var planVersion = Version(0, 1000m, Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m)));
        var request = Request(planVersion, new Dictionary<BillableResource, Count>
        {
            [BillableResource.RuleEvaluation] = new(1000)
        });

        // Act
        var lines = Calculate(request);

        // Assert
        var topUp = lines.Single(x => x.InvoiceLineType == InvoiceLineType.MinimumTopUp);
        topUp.Amount.Should().Be(new LineAmount(900m));

        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(1000m));
    }

    [Fact]
    public void Calculate_GivenUsageAboveTheMinimum_AddsNoTopUpAtAll()
    {
        // Arrange
        var planVersion = Version(0, 100m, Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m)));
        var request = Request(planVersion, new Dictionary<BillableResource, Count>
        {
            [BillableResource.RuleEvaluation] = new(5000)
        });

        // Act
        var lines = Calculate(request);

        // Assert
        lines.Should().NotContain(x => x.InvoiceLineType == InvoiceLineType.MinimumTopUp);
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(500m));
    }

    [Fact]
    public void Calculate_GivenTwoPercentageDiscounts_CompoundsThemInOrder()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m)));
        var effectiveFrom = new EffectiveFrom(DateTimeOffset.UnixEpoch);

        var discounts = new List<ContractDiscount>
        {
            FakeContractDiscount.CreateValid(Fixture) with
            {
                DiscountType = DiscountType.Percentage,
                Value = new DiscountValue(10),
                EffectiveFrom = effectiveFrom,
                CreatedAt = new CreatedAt(DateTimeOffset.UnixEpoch)
            },
            FakeContractDiscount.CreateValid(Fixture) with
            {
                DiscountType = DiscountType.Percentage,
                Value = new DiscountValue(50),
                EffectiveFrom = effectiveFrom,
                CreatedAt = new CreatedAt(DateTimeOffset.UnixEpoch.AddDays(1))
            }
        };

        var request = Request(
            planVersion,
            new Dictionary<BillableResource, Count> { [BillableResource.RuleEvaluation] = new(10000) },
            discounts);

        // Act
        var lines = Calculate(request);

        // Assert
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(450m));
    }

    [Fact]
    public void Calculate_GivenAFixedDiscountLargerThanTheBill_ReducesItToNothing()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m)));

        var discounts = new List<ContractDiscount>
        {
            FakeContractDiscount.CreateValid(Fixture) with
            {
                DiscountType = DiscountType.FixedAmount,
                Value = new DiscountValue(5000)
            }
        };

        var request = Request(
            planVersion,
            new Dictionary<BillableResource, Count> { [BillableResource.RuleEvaluation] = new(1000) },
            discounts);

        // Act
        var lines = Calculate(request);

        // Assert
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(0m));
    }

    [Fact]
    public void Calculate_GivenACommitmentWithBalanceLeft_DrawsTheWholeMonthDownAndBillsNothing()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m)));

        var commitment = FakeCommitment.CreateValid(Fixture) with
        {
            Amount = new CommitmentAmount(12000),
            Drawn = new CommitmentAmount(1000)
        };

        var request = Request(
            planVersion,
            new Dictionary<BillableResource, Count> { [BillableResource.RuleEvaluation] = new(5000) },
            commitment: commitment);

        // Act
        var lines = Calculate(request);

        // Assert
        lines.Should().Contain(x => x.InvoiceLineType == InvoiceLineType.CommitmentDrawdown);
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(0m));
    }

    [Fact]
    public void Calculate_GivenACommitmentTooSmallForTheMonth_BillsTheOverage()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m)));

        var commitment = FakeCommitment.CreateValid(Fixture) with
        {
            Amount = new CommitmentAmount(1000),
            Drawn = new CommitmentAmount(800)
        };

        var request = Request(
            planVersion,
            new Dictionary<BillableResource, Count> { [BillableResource.RuleEvaluation] = new(5000) },
            commitment: commitment);

        // Act
        var lines = Calculate(request);

        // Assert
        lines.Single(x => x.InvoiceLineType == InvoiceLineType.CommitmentDrawdown).Amount.Should().Be(new LineAmount(-200m));
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(300m));
    }

    [Fact]
    public void Calculate_GivenAnAdjustmentAndACommitment_AppliesTheAdjustmentAfterTheDrawdown()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m)));

        var commitment = FakeCommitment.CreateValid(Fixture) with
        {
            Amount = new CommitmentAmount(1000),
            Drawn = new CommitmentAmount(800)
        };

        var adjustments = new List<InvoiceAdjustment>
        {
            FakeInvoiceAdjustment.CreateValid(Fixture) with { Amount = new AdjustmentAmount(-50m) }
        };

        var request = Request(
            planVersion,
            new Dictionary<BillableResource, Count> { [BillableResource.RuleEvaluation] = new(5000) },
            adjustments: adjustments,
            commitment: commitment);

        // Act
        var lines = Calculate(request);

        // Assert
        lines.Single(x => x.InvoiceLineType == InvoiceLineType.CommitmentDrawdown).Amount.Should().Be(new LineAmount(-200m));
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(250m));
    }

    [Fact]
    public void Calculate_GivenAnAdjustment_RecordsWhichOneTheLineCameFrom()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m)));
        var adjustment = FakeInvoiceAdjustment.CreateValid(Fixture) with { Amount = new AdjustmentAmount(25m) };

        var request = Request(planVersion, adjustments: [adjustment]);

        // Act
        var lines = Calculate(request);

        // Assert
        Priced(lines).Should().ContainSingle();
        lines[0].InvoiceAdjustmentId.Should().Be(adjustment.InvoiceAdjustmentId);
    }

    [Fact]
    public void Calculate_GivenFeeMinimumAndDiscount_AppliesThemInThatOrder()
    {
        // Arrange
        var planVersion = Version(200m, 1000m, Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m)));

        var discounts = new List<ContractDiscount>
        {
            FakeContractDiscount.CreateValid(Fixture) with
            {
                DiscountType = DiscountType.Percentage,
                Value = new DiscountValue(10)
            }
        };

        var request = Request(
            planVersion,
            new Dictionary<BillableResource, Count> { [BillableResource.RuleEvaluation] = new(1000) },
            discounts);

        // Act
        var lines = Calculate(request);

        // Assert
        lines.Select(x => x.InvoiceLineType).Should().ContainInOrder(
            InvoiceLineType.Usage,
            InvoiceLineType.PlatformFee,
            InvoiceLineType.MinimumTopUp,
            InvoiceLineType.Discount);

        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(900m));
    }

    [Fact]
    public void Calculate_GivenSubPennyRates_LeavesTheTotalEqualToTheSumOfTheLines()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Flat, 0, Band(0, null, 0.0033m)));
        var request = Request(planVersion, new Dictionary<BillableResource, Count>
        {
            [BillableResource.RuleEvaluation] = new(1001)
        });

        // Act
        var lines = Calculate(request);

        // Assert
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(3.30m));
        lines.Sum(x => x.Amount).Should().Be(PricingCalculator.Total(lines));
    }

    [Fact]
    public void Calculate_GivenVolumeBandsThatDoNotCoverTheQuantity_Throws()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Volume, 0, Band(0, 100, 0.10m)));
        var request = Request(planVersion, new Dictionary<BillableResource, Count>
        {
            [BillableResource.RuleEvaluation] = new(500)
        });

        // Act
        var act = () => Calculate(request);

        // Assert
        act.Should().Throw<InvalidPriceStructureException>();
    }

    [Fact]
    public void Calculate_GivenAPriceWithNoBands_Throws()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Flat, 0));
        var request = Request(planVersion, new Dictionary<BillableResource, Count>
        {
            [BillableResource.RuleEvaluation] = new(1)
        });

        // Act
        var act = () => Calculate(request);

        // Assert
        act.Should().Throw<InvalidPriceStructureException>();
    }

    [Fact]
    public void Calculate_GivenABandWithAFlatAmount_ChargesItOnceAlongsideTheUnits()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Flat, 0, Band(0, null, 0.02m, 500m)));
        var request = Request(planVersion, new Dictionary<BillableResource, Count>
        {
            [BillableResource.RuleEvaluation] = new(10000)
        });

        // Act
        var lines = Calculate(request);

        // Assert
        Priced(lines).Should().ContainSingle();
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(700m));
    }

    [Fact]
    public void Calculate_GivenQuantitiesInAnyOrder_ProducesTheSameLinesInTheSameOrder()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Flat, 0, Band(0, null, 0.01m)));

        var forwards = Request(planVersion, new Dictionary<BillableResource, Count>
        {
            [BillableResource.RuleEvaluation] = new(10),
            [BillableResource.Backtest] = new(20)
        });

        var backwards = Request(planVersion, new Dictionary<BillableResource, Count>
        {
            [BillableResource.Backtest] = new(20),
            [BillableResource.RuleEvaluation] = new(10)
        });

        // Act
        var first = Calculate(forwards);
        var second = Calculate(backwards);

        // Assert
        first.Select(x => x.BillableResource).Should().Equal(second.Select(x => x.BillableResource));
        first.Select(x => (decimal)x.Amount).Should().Equal(second.Select(x => (decimal)x.Amount));
    }

    [Fact]
    public void Calculate_GivenACatchAllCoveringSeveralResources_EntersItsBandsOncePerResource()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Graduated, 0, Band(0, 1000, 0.10m), Band(1000, null, 0.05m)));

        var request = Request(planVersion, new Dictionary<BillableResource, Count>
        {
            [BillableResource.RuleEvaluation] = new(600),
            [BillableResource.Backtest] = new(600)
        });

        // Act
        var lines = Calculate(request);

        // Assert
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(120m));
    }

    [Fact]
    public void Calculate_GivenACatchAllWithAnAllowance_GivesItToEachResourceSeparately()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Flat, 500, Band(0, null, 0.10m)));

        var request = Request(planVersion, new Dictionary<BillableResource, Count>
        {
            [BillableResource.RuleEvaluation] = new(500),
            [BillableResource.Backtest] = new(500)
        });

        // Act
        var lines = Calculate(request);

        // Assert
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(0m));
    }

    [Fact]
    public void Calculate_GivenEveryKindOfLineAtOnce_LeavesTheTotalEqualToTheSumOfTheLines()
    {
        // Arrange
        var planVersion = Version(50, 400, Price(null, PricingModel.Graduated, 100, Band(0, 1000, 0.10m), Band(1000, null, 0.05m)));

        var discounts = new List<ContractDiscount>
        {
            FakeContractDiscount.CreateValid(Fixture) with
            {
                DiscountType = DiscountType.Percentage,
                Value = new DiscountValue(10)
            }
        };

        var adjustments = new List<InvoiceAdjustment>
        {
            FakeInvoiceAdjustment.CreateValid(Fixture) with { Amount = new AdjustmentAmount(-25m) }
        };

        var commitment = FakeCommitment.CreateValid(Fixture) with
        {
            Amount = new CommitmentAmount(1000),
            Drawn = new CommitmentAmount(900)
        };

        var request = Request(
            planVersion,
            new Dictionary<BillableResource, Count> { [BillableResource.RuleEvaluation] = new(5000) },
            discounts,
            adjustments,
            commitment);

        // Act
        var lines = Calculate(request);

        // Assert
        lines.Should().Contain(x => x.InvoiceLineType == InvoiceLineType.Usage);
        lines.Should().Contain(x => x.InvoiceLineType == InvoiceLineType.PlatformFee);
        lines.Should().Contain(x => x.InvoiceLineType == InvoiceLineType.Discount);
        lines.Should().Contain(x => x.InvoiceLineType == InvoiceLineType.CommitmentDrawdown);
        lines.Should().Contain(x => x.InvoiceLineType == InvoiceLineType.Adjustment);

        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(lines.Sum(x => x.Amount)));
    }

    [Fact]
    public void Calculate_NumbersTheLinesInTheOrderTheArithmeticHappened()
    {
        // Arrange
        var planVersion = Version(50, 0, Price(null, PricingModel.Flat, 0, Band(0, null, 0.02m)));

        var discounts = new List<ContractDiscount>
        {
            FakeContractDiscount.CreateValid(Fixture) with
            {
                DiscountType = DiscountType.Percentage,
                Value = new DiscountValue(10)
            }
        };

        var request = Request(
            planVersion,
            new Dictionary<BillableResource, Count> { [BillableResource.RuleEvaluation] = new(1000) },
            discounts);

        // Act
        var lines = Calculate(request);

        // Assert
        lines.Select(x => (int)x.SortOrder).Should().ContainInOrder(Enumerable.Range(0, lines.Count));
    }

    [Fact]
    public void Calculate_GivenNoUsageAndNoFees_ProducesNoLines()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Flat, 0, Band(0, null, 0.02m)));
        var request = Request(planVersion);

        // Act
        var lines = Calculate(request);

        // Assert
        lines.Should().BeEmpty();
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(0m));
    }

    [Fact]
    public void Calculate_GivenACreditLargerThanTheMonth_LeavesTheInvoiceNegative()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m)));

        var adjustments = new List<InvoiceAdjustment>
        {
            FakeInvoiceAdjustment.CreateValid(Fixture) with { Amount = new AdjustmentAmount(-500m) }
        };

        var request = Request(
            planVersion,
            new Dictionary<BillableResource, Count> { [BillableResource.RuleEvaluation] = new(1000) },
            adjustments: adjustments);

        // Act
        var lines = Calculate(request);

        // Assert
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(-400m));
    }

    [Fact]
    public void Calculate_GivenAStandardRate_ChargesItOnTheNetOfEverythingElse()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m)));

        var request = Request(
            planVersion,
            new Dictionary<BillableResource, Count> { [BillableResource.RuleEvaluation] = new(1000) },
            taxDetermination: FakeTaxDetermination.CreateStandard(20m));

        // Act
        var lines = Calculate(request);

        // Assert
        PricingCalculator.Net(lines).Should().Be(new NetAmount(100m));
        PricingCalculator.Tax(lines).Should().Be(new TaxAmount(20m));
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(120m));
        lines.Sum(x => x.Amount).Should().Be(120m);
    }

    [Fact]
    public void Calculate_GivenADiscountAndTax_TaxesWhatIsLeftAfterTheDiscount()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m)));

        var discount = FakeContractDiscount.CreateValid(Fixture) with
        {
            DiscountType = DiscountType.Percentage,
            Value = new DiscountValue(10m)
        };

        var request = Request(
            planVersion,
            new Dictionary<BillableResource, Count> { [BillableResource.RuleEvaluation] = new(1000) },
            discounts: [discount],
            taxDetermination: FakeTaxDetermination.CreateStandard(20m));

        // Act
        var lines = Calculate(request);

        // Assert
        PricingCalculator.Net(lines).Should().Be(new NetAmount(90m));
        PricingCalculator.Tax(lines).Should().Be(new TaxAmount(18m));
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(108m));
    }

    [Fact]
    public void Calculate_GivenNoTaxToCharge_StillWritesTheLineSayingSo()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m)));

        var request = Request(
            planVersion,
            new Dictionary<BillableResource, Count> { [BillableResource.RuleEvaluation] = new(1000) });

        // Act
        var lines = Calculate(request);

        // Assert
        var taxLine = lines.Single(x => x.InvoiceLineType == InvoiceLineType.Tax);
        taxLine.Amount.Should().Be(new LineAmount(0m));
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(100m));
    }

    [Fact]
    public void Calculate_GivenAPlanInAnotherCurrency_ConvertsWhatCameOffThePlan()
    {
        // Arrange
        var planVersion = Version(100m, 0, Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m)));

        var request = Request(
            planVersion,
            new Dictionary<BillableResource, Count> { [BillableResource.RuleEvaluation] = new(1000) },
            conversionRate: new ConversionRate(1.20m));

        // Act
        var lines = Calculate(request);

        // Assert
        Priced(lines).Single(x => x.InvoiceLineType == InvoiceLineType.Usage).Amount.Should().Be(new LineAmount(120m));
        Priced(lines).Single(x => x.InvoiceLineType == InvoiceLineType.PlatformFee).Amount.Should().Be(new LineAmount(120m));
    }

    [Fact]
    public void Calculate_GivenAPlanInAnotherCurrency_LeavesWhatWasAgreedWithTheCustomerAlone()
    {
        // Arrange
        var planVersion = Version(0, 0, Price(null, PricingModel.Flat, 0, Band(0, null, 0.10m)));

        var discount = FakeContractDiscount.CreateValid(Fixture) with
        {
            DiscountType = DiscountType.FixedAmount,
            Value = new DiscountValue(20m)
        };

        var request = Request(
            planVersion,
            new Dictionary<BillableResource, Count> { [BillableResource.RuleEvaluation] = new(1000) },
            discounts: [discount],
            conversionRate: new ConversionRate(1.20m));

        // Act
        var lines = Calculate(request);

        // Assert
        lines.Single(x => x.InvoiceLineType == InvoiceLineType.Discount).Amount.Should().Be(new LineAmount(-20m));
        PricingCalculator.Total(lines).Should().Be(new InvoiceAmount(100m));
    }
}
