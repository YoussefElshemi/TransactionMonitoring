using Core.Enums;
using Core.Exceptions;
using Core.Helpers;
using Core.Models.Plans;
using Core.ValueObjects;
using FluentAssertions;

namespace UnitTests.Tests.Core.Helpers;

public class PriceStructureValidatorTests
{
    private static CreatePlanPriceTierRequest Band(int from, int? to, decimal unit = 0.01m)
    {
        return new CreatePlanPriceTierRequest
        {
            FromQuantity = new FromQuantity(from),
            ToQuantity = to.HasValue ? new ToQuantity(to.Value) : null,
            UnitAmount = new UnitAmount(unit),
            FlatAmount = new FlatAmount(0)
        };
    }

    private static CreatePlanPriceRequest Price(BillableResource? billableResource, params CreatePlanPriceTierRequest[] tiers)
    {
        return Price(billableResource, PricingModel.Graduated, tiers);
    }

    private static CreatePlanPriceRequest Price(BillableResource? billableResource, PricingModel pricingModel, params CreatePlanPriceTierRequest[] tiers)
    {
        return new CreatePlanPriceRequest
        {
            BillableResource = billableResource,
            PricingModel = pricingModel,
            FreeAllowance = new FreeAllowance(0),
            Tiers = [..tiers]
        };
    }

    [Fact]
    public void Validate_GivenBandsThatMeetAndEndUnbounded_DoesNotThrow()
    {
        // Arrange
        var prices = new List<CreatePlanPriceRequest>
        {
            Price(null, Band(0, 1000), Band(1000, 5000), Band(5000, null))
        };

        // Act
        var act = () => PriceStructureValidator.Validate(prices);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_GivenNoPricesAtAll_DoesNotThrow()
    {
        // Act
        var act = () => PriceStructureValidator.Validate([]);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_GivenBandsNotStartingAtZero_Throws()
    {
        // Arrange
        var prices = new List<CreatePlanPriceRequest> { Price(null, Band(100, null)) };

        // Act
        var act = () => PriceStructureValidator.Validate(prices);

        // Assert
        act.Should().Throw<InvalidPriceStructureException>();
    }

    [Fact]
    public void Validate_GivenABreakBetweenBands_Throws()
    {
        // Arrange
        var prices = new List<CreatePlanPriceRequest> { Price(null, Band(0, 1000), Band(5000, null)) };

        // Act
        var act = () => PriceStructureValidator.Validate(prices);

        // Assert
        act.Should().Throw<InvalidPriceStructureException>();
    }

    [Fact]
    public void Validate_GivenABoundedTopBand_Throws()
    {
        // Arrange
        var prices = new List<CreatePlanPriceRequest> { Price(null, Band(0, 1000)) };

        // Act
        var act = () => PriceStructureValidator.Validate(prices);

        // Assert
        act.Should().Throw<InvalidPriceStructureException>();
    }

    [Fact]
    public void Validate_GivenAnUnboundedBandBelowTheTop_Throws()
    {
        // Arrange
        var prices = new List<CreatePlanPriceRequest> { Price(null, Band(0, null), Band(1000, null)) };

        // Act
        var act = () => PriceStructureValidator.Validate(prices);

        // Assert
        act.Should().Throw<InvalidPriceStructureException>();
    }

    [Fact]
    public void Validate_GivenAPriceWithNoBands_Throws()
    {
        // Arrange
        var prices = new List<CreatePlanPriceRequest> { Price(null) };

        // Act
        var act = () => PriceStructureValidator.Validate(prices);

        // Assert
        act.Should().Throw<InvalidPriceStructureException>();
    }

    [Fact]
    public void Validate_GivenTheSameResourcePricedTwice_Throws()
    {
        // Arrange
        var prices = new List<CreatePlanPriceRequest>
        {
            Price(BillableResource.RuleEvaluation, Band(0, null)),
            Price(BillableResource.RuleEvaluation, Band(0, null))
        };

        // Act
        var act = () => PriceStructureValidator.Validate(prices);

        // Assert
        act.Should().Throw<InvalidPriceStructureException>();
    }

    [Fact]
    public void Validate_GivenTwoCatchAllPrices_Throws()
    {
        // Arrange
        var prices = new List<CreatePlanPriceRequest>
        {
            Price(null, Band(0, null)),
            Price(null, Band(0, null))
        };

        // Act
        var act = () => PriceStructureValidator.Validate(prices);

        // Assert
        act.Should().Throw<InvalidPriceStructureException>();
    }

    [Fact]
    public void Validate_GivenBandsOutOfOrder_DoesNotThrow()
    {
        // Arrange
        var prices = new List<CreatePlanPriceRequest>
        {
            Price(null, Band(1000, null), Band(0, 1000))
        };

        // Act
        var act = () => PriceStructureValidator.Validate(prices);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_GivenAFlatPriceWithMoreThanOneBand_Throws()
    {
        // Arrange
        var prices = new List<CreatePlanPriceRequest>
        {
            Price(null, PricingModel.Flat, Band(0, 1000), Band(1000, null))
        };

        // Act
        var act = () => PriceStructureValidator.Validate(prices);

        // Assert
        act.Should().Throw<InvalidPriceStructureException>();
    }

    [Fact]
    public void Validate_GivenAFlatPriceWithOneUnboundedBand_DoesNotThrow()
    {
        // Arrange
        var prices = new List<CreatePlanPriceRequest>
        {
            Price(null, PricingModel.Flat, Band(0, null))
        };

        // Act
        var act = () => PriceStructureValidator.Validate(prices);

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(PricingModel.Graduated)]
    [InlineData(PricingModel.Volume)]
    public void Validate_GivenABandedModelWithSeveralBands_DoesNotThrow(PricingModel pricingModel)
    {
        // Arrange
        var prices = new List<CreatePlanPriceRequest>
        {
            Price(null, pricingModel, Band(0, 1000), Band(1000, null))
        };

        // Act
        var act = () => PriceStructureValidator.Validate(prices);

        // Assert
        act.Should().NotThrow();
    }
}
