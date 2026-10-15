using Core.Configs;
using Core.Enums;
using Core.Helpers;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class DunningLadderTests : BaseTestClass
{
    private static readonly PaymentConfig PaymentConfig = new()
    {
        GraceDays = 3,
        NoticeIntervalDays = [0, 7, 14],
        RestrictionProposalDays = 14
    };

    private static readonly DateTimeOffset DueAt = new(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Open_GivenADueDate_StartsInGraceAndWaitsTheGraceWindow()
    {
        // Arrange
        // Act
        var (stage, nextAttemptAt) = DunningLadder.Open(DueAt, PaymentConfig);

        // Assert
        stage.Should().Be(DunningStage.Grace);
        nextAttemptAt.Should().Be(DueAt.AddDays(3));
    }

    [Theory]
    [InlineData(DunningStage.Grace, DunningStage.FirstNotice)]
    [InlineData(DunningStage.FirstNotice, DunningStage.SecondNotice)]
    [InlineData(DunningStage.SecondNotice, DunningStage.FinalNotice)]
    [InlineData(DunningStage.FinalNotice, DunningStage.RestrictionProposed)]
    public void Next_GivenARung_ReturnsTheOneAboveIt(DunningStage stage, DunningStage expected)
    {
        // Arrange
        // Act
        var next = DunningLadder.Next(stage);

        // Assert
        next.Should().Be(expected);
    }

    [Fact]
    public void Next_GivenRestrictionProposed_HasNowhereToGo()
    {
        // Arrange
        // Act
        var next = DunningLadder.Next(DunningStage.RestrictionProposed);

        // Assert
        next.Should().BeNull();
    }

    [Fact]
    public void NextAttemptAt_AcrossTheLadder_WidensTheGapBetweenNotices()
    {
        // Arrange
        var asOf = DueAt.AddDays(3);

        // Act
        var afterFirst = DunningLadder.NextAttemptAt(DunningStage.FirstNotice, asOf, PaymentConfig);
        var afterSecond = DunningLadder.NextAttemptAt(DunningStage.SecondNotice, asOf, PaymentConfig);
        var afterFinal = DunningLadder.NextAttemptAt(DunningStage.FinalNotice, asOf, PaymentConfig);

        // Assert
        (afterFirst - asOf).Should().Be(TimeSpan.FromDays(7));
        (afterSecond - asOf).Should().Be(TimeSpan.FromDays(7));
        (afterFinal - asOf).Should().Be(TimeSpan.FromDays(14));
    }

    [Fact]
    public void NextAttemptAt_GivenARunThatWasDownForAWeek_StillLeavesAFullGapBeforeTheNextNotice()
    {
        // Arrange
        var lateRun = DueAt.AddDays(30);

        // Act
        var nextAttemptAt = DunningLadder.NextAttemptAt(DunningStage.FirstNotice, lateRun, PaymentConfig);

        // Assert
        nextAttemptAt.Should().Be(lateRun.AddDays(7));
    }

    [Fact]
    public void NextAttemptAt_GivenFewerIntervalsThanRungs_StillLeavesAGap()
    {
        // Arrange
        var sparse = new PaymentConfig { NoticeIntervalDays = [0], RestrictionProposalDays = 14 };
        var asOf = DueAt;

        // Act
        var afterFirst = DunningLadder.NextAttemptAt(DunningStage.FirstNotice, asOf, sparse);
        var afterSecond = DunningLadder.NextAttemptAt(DunningStage.SecondNotice, asOf, sparse);

        // Assert
        afterFirst.Should().BeAfter(asOf);
        afterSecond.Should().BeAfter(asOf);
    }

    [Theory]
    [InlineData(DunningStage.Grace, false)]
    [InlineData(DunningStage.FirstNotice, true)]
    [InlineData(DunningStage.SecondNotice, true)]
    [InlineData(DunningStage.FinalNotice, true)]
    [InlineData(DunningStage.RestrictionProposed, false)]
    public void Collects_GivenARung_SaysWhetherItAlsoAsksForTheMoney(DunningStage stage, bool expected)
    {
        // Arrange
        // Act
        var collects = DunningLadder.Collects(stage);

        // Assert
        collects.Should().Be(expected);
    }

    [Fact]
    public void Next_WalkedFromGrace_TerminatesAtRestrictionProposed()
    {
        // Arrange
        var stage = DunningStage.Grace;
        var rungs = 0;

        // Act
        while (DunningLadder.Next(stage) is { } next)
        {
            stage = next;
            rungs++;
        }

        // Assert
        stage.Should().Be(DunningStage.RestrictionProposed);
        rungs.Should().Be(4);
    }
}
