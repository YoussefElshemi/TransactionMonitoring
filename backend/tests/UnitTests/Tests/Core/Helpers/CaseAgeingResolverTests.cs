using Core.Enums;
using Core.Helpers;
using Core.Models.Cases;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;

namespace UnitTests.Tests.Core.Helpers;

public class CaseAgeingResolverTests : BaseTestClass
{
    private static readonly DateTimeOffset OpenedAt = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0, CaseAgeBucket.Fresh)]
    [InlineData(29, CaseAgeBucket.Fresh)]
    [InlineData(30, CaseAgeBucket.Ageing)]
    [InlineData(47, CaseAgeBucket.Ageing)]
    [InlineData(48, CaseAgeBucket.DueSoon)]
    [InlineData(59, CaseAgeBucket.DueSoon)]
    [InlineData(60, CaseAgeBucket.Breached)]
    [InlineData(600, CaseAgeBucket.Breached)]
    public void Resolve_BucketsOnTheFractionOfTheWindowElapsed(int minutesElapsed, CaseAgeBucket expected)
    {
        // Arrange
        var @case = Case(dueInMinutes: 60);

        // Act
        var caseAgeing = CaseAgeingResolver.Resolve(@case, OpenedAt.AddMinutes(minutesElapsed));

        // Assert
        caseAgeing.Bucket.Should().Be(expected);
    }

    [Fact]
    public void Resolve_BucketsTheSameFractionAlikeAcrossVeryDifferentWindows()
    {
        // Arrange
        var shortCase = Case(dueInMinutes: 60);
        var longCase = Case(dueInMinutes: 10080);

        // Act
        var shortAgeing = CaseAgeingResolver.Resolve(shortCase, OpenedAt.AddMinutes(54));
        var longAgeing = CaseAgeingResolver.Resolve(longCase, OpenedAt.AddMinutes(9072));

        // Assert
        shortAgeing.Bucket.Should().Be(CaseAgeBucket.DueSoon);
        longAgeing.Bucket.Should().Be(CaseAgeBucket.DueSoon);
    }

    [Fact]
    public void Resolve_ReportsTimeToDueAsNegativeOnceTheDeadlineHasPassed()
    {
        // Arrange
        var @case = Case(dueInMinutes: 60);

        // Act
        var caseAgeing = CaseAgeingResolver.Resolve(@case, OpenedAt.AddMinutes(90));

        // Assert
        caseAgeing.TimeToDue.Should().Be(TimeSpan.FromMinutes(-30));
        caseAgeing.Age.Should().Be(TimeSpan.FromMinutes(90));
        ((bool)caseAgeing.IsBreached).Should().BeTrue();
    }

    [Fact]
    public void Resolve_ForAResolvedCase_MeasuresToTheResolutionAndNotToNow()
    {
        // Arrange
        var @case = Case(dueInMinutes: 60) with
        {
            Status = CaseStatus.Approved,
            ResolvedAt = new ResolvedAt(OpenedAt.AddMinutes(30))
        };

        // Act
        var caseAgeing = CaseAgeingResolver.Resolve(@case, OpenedAt.AddDays(30));

        // Assert
        caseAgeing.Age.Should().Be(TimeSpan.FromMinutes(30));
        ((bool)caseAgeing.IsBreached).Should().BeFalse();
        caseAgeing.Bucket.Should().Be(CaseAgeBucket.Ageing);
    }

    [Fact]
    public void Resolve_ForACaseResolvedLate_StaysBreachedForever()
    {
        // Arrange
        var @case = Case(dueInMinutes: 60) with
        {
            Status = CaseStatus.Rejected,
            ResolvedAt = new ResolvedAt(OpenedAt.AddMinutes(120))
        };

        // Act
        var caseAgeing = CaseAgeingResolver.Resolve(@case, OpenedAt.AddDays(30));

        // Assert
        ((bool)caseAgeing.IsBreached).Should().BeTrue();
        caseAgeing.Bucket.Should().Be(CaseAgeBucket.Breached);
    }

    [Fact]
    public void Resolve_ForANonTerminalDeskState_KeepsMeasuringAgainstTheClock()
    {
        // Arrange
        var @case = Case(dueInMinutes: 60) with
        {
            Status = CaseStatus.PendingInformation,
            ResolvedAt = null
        };

        // Act
        var caseAgeing = CaseAgeingResolver.Resolve(@case, OpenedAt.AddMinutes(90));

        // Assert
        ((bool)caseAgeing.IsBreached).Should().BeTrue();
    }

    [Fact]
    public void Resolve_WithANonPositiveWindow_ReadsAsBreached()
    {
        // Arrange
        var @case = Case(dueInMinutes: 0);

        // Act
        var caseAgeing = CaseAgeingResolver.Resolve(@case, OpenedAt);

        // Assert
        ((bool)caseAgeing.IsBreached).Should().BeTrue();
        caseAgeing.Bucket.Should().Be(CaseAgeBucket.Breached);
    }

    private Case Case(int dueInMinutes)
    {
        return FakeCase.CreateValid(Fixture) with
        {
            Status = CaseStatus.Pending,
            CreatedAt = new CreatedAt(OpenedAt),
            DueAt = new DueAt(OpenedAt.AddMinutes(dueInMinutes)),
            ResolvedAt = null
        };
    }
}
