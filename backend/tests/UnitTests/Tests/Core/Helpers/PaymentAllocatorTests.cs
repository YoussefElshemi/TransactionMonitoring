using Core.Helpers;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class PaymentAllocatorTests : BaseTestClass
{
    [Fact]
    public void Allocatable_GivenABalanceEqualToTheOutstanding_AppliesAllOfIt()
    {
        // Arrange
        // Act
        var allocatable = PaymentAllocator.Allocatable(new BalanceAmount(100m), new OutstandingAmount(100m));

        // Assert
        allocatable.Should().Be(100m);
    }

    [Fact]
    public void Allocatable_GivenMoreCashThanTheInvoiceNeeds_StopsAtTheOutstanding()
    {
        // Arrange
        // Act
        var allocatable = PaymentAllocator.Allocatable(new BalanceAmount(150m), new OutstandingAmount(100m));

        // Assert
        allocatable.Should().Be(100m);
    }

    [Fact]
    public void Allocatable_GivenLessCashThanTheInvoiceNeeds_AppliesWhatThereIs()
    {
        // Arrange
        // Act
        var allocatable = PaymentAllocator.Allocatable(new BalanceAmount(40m), new OutstandingAmount(100m));

        // Assert
        allocatable.Should().Be(40m);
    }

    [Fact]
    public void Allocatable_GivenANegativeBalance_AppliesNothing()
    {
        // Arrange
        // Act
        var allocatable = PaymentAllocator.Allocatable(new BalanceAmount(-25m), new OutstandingAmount(100m));

        // Assert
        allocatable.Should().Be(0m);
    }

    [Fact]
    public void Allocatable_GivenNothingOutstanding_AppliesNothing()
    {
        // Arrange
        // Act
        var allocatable = PaymentAllocator.Allocatable(new BalanceAmount(500m), new OutstandingAmount(0m));

        // Assert
        allocatable.Should().Be(0m);
    }

    [Fact]
    public void Allocatable_GivenARequestedFigure_IsStillCappedByBothSides()
    {
        // Arrange
        // Act
        var cappedByRequest = PaymentAllocator.Allocatable(new BalanceAmount(150m), new OutstandingAmount(100m), 30m);
        var cappedByOutstanding = PaymentAllocator.Allocatable(new BalanceAmount(150m), new OutstandingAmount(100m), 500m);
        var cappedByBalance = PaymentAllocator.Allocatable(new BalanceAmount(20m), new OutstandingAmount(100m), 500m);

        // Assert
        cappedByRequest.Should().Be(30m);
        cappedByOutstanding.Should().Be(100m);
        cappedByBalance.Should().Be(20m);
    }

    [Fact]
    public void Allocatable_GivenANegativeRequest_AppliesNothing()
    {
        // Arrange
        // Act
        var allocatable = PaymentAllocator.Allocatable(new BalanceAmount(150m), new OutstandingAmount(100m), -50m);

        // Assert
        allocatable.Should().Be(0m);
    }
}
