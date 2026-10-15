using Core.Helpers;
using Core.ValueObjects;
using FluentAssertions;

namespace UnitTests.Tests.Core.Helpers;

public class BillingPeriodTests
{
    [Fact]
    public void EndOf_GivenAMonth_ReturnsItsLastInstantInUtc()
    {
        // Act
        var endOf = BillingPeriod.EndOf(new Month(2), new Year(2026));

        // Assert
        endOf.Should().Be(new DateTimeOffset(2026, 2, 28, 23, 59, 59, TimeSpan.Zero));
    }

    [Fact]
    public void EndOf_GivenFebruaryInALeapYear_ReturnsTheTwentyNinth()
    {
        // Act
        var endOf = BillingPeriod.EndOf(new Month(2), new Year(2028));

        // Assert
        endOf.Should().Be(new DateTimeOffset(2028, 2, 29, 23, 59, 59, TimeSpan.Zero));
    }

    [Fact]
    public void Key_GivenConsecutiveMonthsAcrossAYearBoundary_Increases()
    {
        // Act
        var december = BillingPeriod.Key(new Month(12), new Year(2026));
        var january = BillingPeriod.Key(new Month(1), new Year(2027));

        // Assert
        january.Should().Be(december + 1);
    }

    [Fact]
    public void KeysWithin_GivenAWholeCalendarYear_CoversJanuaryToDecember()
    {
        // Arrange
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var (firstKey, lastKey) = BillingPeriod.KeysWithin(from, to);

        // Assert
        firstKey.Should().Be(BillingPeriod.Key(new Month(1), new Year(2026)));
        lastKey.Should().Be(BillingPeriod.Key(new Month(12), new Year(2026)));
    }

    [Fact]
    public void KeysWithin_GivenATermStartingMidMonth_StillCoversThatMonth()
    {
        // Arrange
        var from = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var (firstKey, _) = BillingPeriod.KeysWithin(from, to);

        // Assert
        firstKey.Should().Be(BillingPeriod.Key(new Month(3), new Year(2026)));
    }

    [Fact]
    public void KeysWithin_GivenATermEndingAtAMonthsLastInstant_ExcludesThatMonth()
    {
        // Arrange
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = BillingPeriod.EndOf(new Month(6), new Year(2026));

        // Act
        var (_, lastKey) = BillingPeriod.KeysWithin(from, to);

        // Assert
        lastKey.Should().Be(BillingPeriod.Key(new Month(5), new Year(2026)));
    }

    [Fact]
    public void KeysWithin_GivenATermOutlastingAMonthsLastInstant_IncludesThatMonth()
    {
        // Arrange
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = BillingPeriod.EndOf(new Month(6), new Year(2026)).AddSeconds(1);

        // Act
        var (_, lastKey) = BillingPeriod.KeysWithin(from, to);

        // Assert
        lastKey.Should().Be(BillingPeriod.Key(new Month(6), new Year(2026)));
    }

    [Fact]
    public void KeysWithin_GivenATermStartingAfterItsMonthHasEnded_ExcludesThatMonth()
    {
        // Arrange
        var from = BillingPeriod.EndOf(new Month(3), new Year(2026)).AddSeconds(1);
        var to = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var (firstKey, _) = BillingPeriod.KeysWithin(from, to);

        // Assert
        firstKey.Should().Be(BillingPeriod.Key(new Month(4), new Year(2026)));
    }

    [Fact]
    public void KeysWithin_GivenATermStartingAfterDecemberHasEnded_RollsIntoTheNextYear()
    {
        // Arrange
        var from = BillingPeriod.EndOf(new Month(12), new Year(2026)).AddSeconds(1);
        var to = new DateTimeOffset(2027, 7, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var (firstKey, _) = BillingPeriod.KeysWithin(from, to);

        // Assert
        firstKey.Should().Be(BillingPeriod.Key(new Month(1), new Year(2027)));
    }

    [Fact]
    public void KeysWithin_GivenATermTooShortToCoverAnyMonth_ReturnsAnEmptyRange()
    {
        // Arrange
        var from = new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 3, 20, 0, 0, 0, TimeSpan.Zero);

        // Act
        var (firstKey, lastKey) = BillingPeriod.KeysWithin(from, to);

        // Assert
        lastKey.Should().BeLessThan(firstKey);
    }
}
