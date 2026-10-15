using Core.Configs;
using Core.Enums;
using Core.Helpers;
using Core.Models.Revenue;
using Core.ValueObjects;
using FluentAssertions;

namespace UnitTests.Tests.Core.Helpers;

public class CustomerHealthScorerTests
{
    private static readonly RevenueConfig RevenueConfig = new();

    [Fact]
    public void Score_GivenNothingPointingTheWrongWay_ScoresFullMarksAndReportsNoDeduction()
    {
        // Act
        var (score, band, signals) = Score(usageTrend: new TrendRate(5m));

        // Assert
        ((int)score).Should().Be(100);
        band.Should().Be(HealthBand.Healthy);
        signals.Should().BeEmpty();
    }

    [Fact]
    public void Score_GivenTrafficThatDoubled_DoesNotScoreAboveFullMarks()
    {
        // Act
        var (score, _, _) = Score(usageTrend: new TrendRate(100m));

        // Assert
        ((int)score).Should().Be(100);
    }

    [Fact]
    public void Score_GivenTrafficFallingPastTheThreshold_DeductsAndSaysWhy()
    {
        // Act
        var (score, _, signals) = Score(usageTrend: new TrendRate(-30m));

        // Assert
        ((int)score).Should().Be(80);
        signals.Should().ContainSingle(x => x.Kind == HealthSignalKind.UsageDecline);
        signals[0].Detail.ToString().Should().Contain("30.0%");
    }

    [Fact]
    public void Score_GivenTrafficThatHalved_DeductsMoreThanAnOrdinaryDecline()
    {
        // Act
        var (severe, _, _) = Score(usageTrend: new TrendRate(-60m));
        var (ordinary, _, _) = Score(usageTrend: new TrendRate(-30m));

        // Assert
        ((int)severe).Should().BeLessThan(ordinary);
    }

    [Fact]
    public void Score_GivenNoEarlierTrafficToCompareAgainst_RecordsTheGapAndDeductsNothing()
    {
        // Act
        var (score, _, signals) = Score(usageTrend: null);

        // Assert
        ((int)score).Should().Be(100);
        signals.Should().ContainSingle(x => x.Kind == HealthSignalKind.NoBaseline);
        ((int)signals[0].Penalty).Should().Be(0);
    }

    [Fact]
    public void Score_GivenErrorsThatDoubledButStayedTiny_DeductsNothing()
    {
        // Act
        var (score, _, signals) = Score(
            usageTrend: new TrendRate(0m),
            recentErrorRate: new ErrorRate(0.2m),
            previousErrorRate: new ErrorRate(0.1m));

        // Assert
        ((int)score).Should().Be(100);
        signals.Should().NotContain(x => x.Kind == HealthSignalKind.ErrorRateRise);
    }

    [Fact]
    public void Score_GivenErrorsRisingByMoreThanAPoint_DeductsAndNamesBothFigures()
    {
        // Act
        var (score, _, signals) = Score(
            usageTrend: new TrendRate(0m),
            recentErrorRate: new ErrorRate(4m),
            previousErrorRate: new ErrorRate(0.5m));

        // Assert
        ((int)score).Should().Be(85);
        signals.Should().ContainSingle(x => x.Kind == HealthSignalKind.ErrorRateRise);
        signals[0].Detail.ToString().Should().Contain("4.00%").And.Contain("0.50%");
    }

    [Fact]
    public void Score_GivenNothingMeteredForLongerThanTheSilenceWindow_DeductsForSilence()
    {
        // Act
        var (score, _, signals) = Score(
            usageTrend: new TrendRate(0m),
            daysSinceLastUsage: new DaysElapsed(30));

        // Assert
        ((int)score).Should().Be(70);
        signals.Should().ContainSingle(x => x.Kind == HealthSignalKind.Silence);
    }

    [Fact]
    public void Score_GivenACustomerWhoHasNeverSentAnything_DoesNotDeductForSilence()
    {
        // Act
        var (_, _, signals) = Score(usageTrend: null, daysSinceLastUsage: null);

        // Assert
        signals.Should().NotContain(x => x.Kind == HealthSignalKind.Silence);
    }

    [Fact]
    public void Score_GivenACustomerAtTheEndOfTheLadder_DeductsMoreThanOneInsideTheGraceWindow()
    {
        // Act
        var (grace, _, _) = Score(usageTrend: new TrendRate(0m), arrearsStage: DunningStage.Grace);
        var (final, _, _) = Score(usageTrend: new TrendRate(0m), arrearsStage: DunningStage.RestrictionProposed);

        // Assert
        ((int)final).Should().BeLessThan(grace);
    }

    [Fact]
    public void Score_GivenEnoughGoingWrongToExceedAHundred_FloorsAtZeroRatherThanThrowing()
    {
        // Act
        var (score, band, _) = Score(
            usageTrend: new TrendRate(-90m),
            recentErrorRate: new ErrorRate(50m),
            previousErrorRate: new ErrorRate(0m),
            daysSinceLastUsage: new DaysElapsed(90),
            arrearsStage: DunningStage.RestrictionProposed);

        // Assert
        ((int)score).Should().Be(0);
        band.Should().Be(HealthBand.AtRisk);
    }

    [Theory]
    [InlineData(0, HealthBand.Healthy)]
    [InlineData(-30, HealthBand.Healthy)]
    [InlineData(-60, HealthBand.Watch)]
    public void Score_GivenADecline_LandsInTheBandItsScoreFallsIn(int trend, HealthBand expected)
    {
        // Act
        var (_, band, _) = Score(usageTrend: new TrendRate(trend));

        // Assert
        band.Should().Be(expected);
    }

    private static (HealthScore Score, HealthBand Band, List<HealthSignal> Signals) Score(
        TrendRate? usageTrend,
        ErrorRate? recentErrorRate = null,
        ErrorRate? previousErrorRate = null,
        DaysElapsed? daysSinceLastUsage = null,
        DunningStage? arrearsStage = null)
    {
        return CustomerHealthScorer.Score(
            usageTrend,
            recentErrorRate ?? new ErrorRate(0m),
            previousErrorRate ?? new ErrorRate(0m),
            daysSinceLastUsage,
            arrearsStage,
            RevenueConfig);
    }
}
