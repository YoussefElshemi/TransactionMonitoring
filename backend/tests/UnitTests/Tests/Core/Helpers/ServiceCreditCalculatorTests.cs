using Core.Helpers;
using Core.Models.Availability;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class ServiceCreditCalculatorTests : BaseTestClass
{
    [Fact]
    public void Resolve_GivenAMonthThatMetItsTarget_OwesNothing()
    {
        // Arrange
        var bands = Schedule();

        // Act
        var creditPercentage = ServiceCreditCalculator.Resolve(bands, new UptimePercentage(99.95m));

        // Assert
        ((decimal)creditPercentage).Should().Be(0m);
    }

    [Fact]
    public void Resolve_GivenAMonthInsideTheFirstBand_OwesThatBand()
    {
        // Arrange
        var bands = Schedule();

        // Act
        var creditPercentage = ServiceCreditCalculator.Resolve(bands, new UptimePercentage(99.5m));

        // Assert
        ((decimal)creditPercentage).Should().Be(10m);
    }

    [Fact]
    public void Resolve_GivenAMonthBelowEveryBand_OwesTheLargestCredit()
    {
        // Arrange
        var bands = Schedule();

        // Act
        var creditPercentage = ServiceCreditCalculator.Resolve(bands, new UptimePercentage(90m));

        // Assert
        ((decimal)creditPercentage).Should().Be(50m);
    }

    [Fact]
    public void Resolve_GivenBandsOutOfOrder_StillOwesTheLargestCredit()
    {
        // Arrange
        var bands = Schedule().AsEnumerable().Reverse().ToList();

        // Act
        var creditPercentage = ServiceCreditCalculator.Resolve(bands, new UptimePercentage(94m));

        // Assert
        ((decimal)creditPercentage).Should().Be(50m);
    }

    [Fact]
    public void Resolve_GivenAMonthExactlyAtABound_IsAboveThatBand()
    {
        // Arrange
        var bands = Schedule();

        // Act
        var creditPercentage = ServiceCreditCalculator.Resolve(bands, new UptimePercentage(99.9m));

        // Assert
        ((decimal)creditPercentage).Should().Be(0m);
    }

    [Fact]
    public void Resolve_GivenNoScheduleAtAll_OwesNothing()
    {
        // Act
        var creditPercentage = ServiceCreditCalculator.Resolve([], new UptimePercentage(50m));

        // Assert
        ((decimal)creditPercentage).Should().Be(0m);
    }

    [Fact]
    public void Calculate_GivenAPercentageAndAnInvoicedNet_TakesThatPercentageOfIt()
    {
        // Act
        var amount = ServiceCreditCalculator.Calculate(new CreditPercentage(10m), new NetAmount(1234.56m));

        // Assert
        ((decimal)amount).Should().Be(123.46m);
    }

    [Fact]
    public void Calculate_GivenNothingOwed_IsZero()
    {
        // Act
        var amount = ServiceCreditCalculator.Calculate(new CreditPercentage(0m), new NetAmount(1000m));

        // Assert
        ((decimal)amount).Should().Be(0m);
    }

    private static List<ServiceCreditBand> Schedule()
    {
        return
        [
            Band(99.9m, 10m),
            Band(99m, 25m),
            Band(95m, 50m)
        ];
    }

    private static ServiceCreditBand Band(decimal below, decimal credit)
    {
        return new ServiceCreditBand
        {
            ServiceCreditBandId = new ServiceCreditBandId(Guid.NewGuid()),
            AvailabilityCommitmentId = new AvailabilityCommitmentId(Guid.NewGuid()),
            BelowUptimePercentage = new UptimePercentage(below),
            CreditPercentage = new CreditPercentage(credit),
            CreatedAt = new CreatedAt(TestTimeConstants.Today),
            UpdatedAt = new UpdatedAt(TestTimeConstants.Today)
        };
    }
}
