using Core.Enums;
using Core.Helpers;
using Core.Models.Rules;
using FluentAssertions;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Core.Helpers;

public class VelocityWindowResolverTests : BaseTestClass
{
    private static readonly DateTimeOffset Now = new(2026, 8, 19, 14, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Resolve_GivenARollingWindow_CountsBackFromTheMomentOfJudgement()
    {
        // Arrange
        var window = new VelocityWindowModel { Kind = VelocityWindowKind.Rolling, Unit = VelocityWindowUnit.Hour, Length = 24 };

        // Act
        var (from, to) = VelocityWindowResolver.Resolve(window, Now);

        // Assert
        from.Should().Be(Now.AddHours(-24));
        to.Should().Be(Now);
    }

    [Fact]
    public void Resolve_GivenACalendarDay_StartsAtMidnight()
    {
        // Arrange
        var window = new VelocityWindowModel { Kind = VelocityWindowKind.Calendar, Unit = VelocityWindowUnit.Day };

        // Act
        var (from, to) = VelocityWindowResolver.Resolve(window, Now);

        // Assert
        from.Should().Be(new DateTimeOffset(2026, 8, 19, 0, 0, 0, TimeSpan.Zero));
        to.Should().Be(Now);
    }

    [Fact]
    public void Resolve_GivenACalendarWeek_StartsOnMonday()
    {
        // Arrange
        var window = new VelocityWindowModel { Kind = VelocityWindowKind.Calendar, Unit = VelocityWindowUnit.Week };

        // Act
        var (from, _) = VelocityWindowResolver.Resolve(window, Now);

        // Assert
        from.Should().Be(new DateTimeOffset(2026, 8, 17, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Resolve_GivenACalendarMonth_StartsOnTheFirst()
    {
        // Arrange
        var window = new VelocityWindowModel { Kind = VelocityWindowKind.Calendar, Unit = VelocityWindowUnit.Month };

        // Act
        var (from, _) = VelocityWindowResolver.Resolve(window, Now);

        // Assert
        from.Should().Be(new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Resolve_GivenACalendarDayInAZoneAheadOfUtc_StartsAtThatZonesMidnight()
    {
        // Arrange
        var window = new VelocityWindowModel
        {
            Kind = VelocityWindowKind.Calendar,
            Unit = VelocityWindowUnit.Day,
            TimeZone = "Asia/Tokyo"
        };

        // Act
        var (from, _) = VelocityWindowResolver.Resolve(window, Now);

        // Assert
        from.Should().Be(new DateTimeOffset(2026, 8, 19, 0, 0, 0, TimeSpan.FromHours(9)));
    }

    [Fact]
    public void Resolve_GivenAZoneTheHostDoesNotKnow_FallsBackToUtc()
    {
        // Arrange
        var window = new VelocityWindowModel
        {
            Kind = VelocityWindowKind.Calendar,
            Unit = VelocityWindowUnit.Day,
            TimeZone = "Nowhere/Atlantis"
        };

        // Act
        var (from, _) = VelocityWindowResolver.Resolve(window, Now);

        // Assert
        from.Should().Be(new DateTimeOffset(2026, 8, 19, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Widest_GivenSeveralWindows_ReachesBackAsFarAsTheWidest()
    {
        // Arrange
        var windows = new List<VelocityWindowModel>
        {
            new() { Kind = VelocityWindowKind.Rolling, Unit = VelocityWindowUnit.Hour, Length = 1 },
            new() { Kind = VelocityWindowKind.Rolling, Unit = VelocityWindowUnit.Day, Length = 30 },
            new() { Kind = VelocityWindowKind.Calendar, Unit = VelocityWindowUnit.Day }
        };

        // Act
        var from = VelocityWindowResolver.Widest(windows, Now);

        // Assert
        from.Should().Be(Now.AddDays(-30));
    }

    [Theory]
    [InlineData(VelocityWindowUnit.Day, 366)]
    [InlineData(VelocityWindowUnit.Week, 53)]
    [InlineData(VelocityWindowUnit.Month, 12)]
    public void MaxLength_BoundsAWindowToAboutAYear(VelocityWindowUnit unit, int expected)
    {
        // Act
        var maxLength = VelocityWindowResolver.MaxLength(unit);

        // Assert
        maxLength.Should().Be(expected);
    }
}
