using Core.Enums;
using Core.Helpers;
using Core.Models.NotificationSubscriptions;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.NotificationSubscriptions;

namespace UnitTests.Tests.Core.Helpers;

public class NotificationSubscriptionMatcherTests : BaseTestClass
{
    [Theory]
    [InlineData(RuleSeverity.Critical, true)]
    [InlineData(RuleSeverity.High, true)]
    [InlineData(RuleSeverity.Medium, false)]
    [InlineData(RuleSeverity.Info, false)]
    public void ForSeverity_GivenAFloor_TakesAtOrAboveIt(RuleSeverity severity, bool expected)
    {
        // Arrange
        var subscription = Subscription(minimumSeverity: RuleSeverity.High);

        // Act
        var matched = new List<NotificationSubscription> { subscription }.ForSeverity(severity);

        // Assert
        matched.Should().HaveCount(expected ? 1 : 0);
    }

    [Fact]
    public void ForSeverity_GivenNoFloor_TakesEverySeverity()
    {
        // Arrange
        var subscription = Subscription(minimumSeverity: null);

        // Act
        var matched = new List<NotificationSubscription> { subscription }.ForSeverity(RuleSeverity.Info);

        // Assert
        matched.Should().ContainSingle();
    }

    [Fact]
    public void ForBreach_TakesOnlyTheSubscriptionsThatAskedForBreaches()
    {
        // Arrange
        var wants = Subscription(notifyOnBreach: true);
        var doesNot = Subscription(notifyOnBreach: false);

        // Act
        var matched = new List<NotificationSubscription> { wants, doesNot }.ForBreach();

        // Assert
        matched.Should().ContainSingle();
        matched[0].NotificationSubscriptionId.Should().Be(wants.NotificationSubscriptionId);
    }

    [Fact]
    public void ForBreach_GivenAHighSeverityFloor_StillTakesALowSeverityBreach()
    {
        // Arrange
        var subscription = Subscription(minimumSeverity: RuleSeverity.Critical, notifyOnBreach: true);

        // Act
        var matched = new List<NotificationSubscription> { subscription }.ForBreach();

        // Assert
        matched.Should().ContainSingle();
    }

    private NotificationSubscription Subscription(RuleSeverity? minimumSeverity = null, bool notifyOnBreach = false)
    {
        return FakeNotificationSubscription.CreateValid(Fixture) with
        {
            MinimumSeverity = minimumSeverity,
            NotifyOnBreach = new NotifyOnBreach(notifyOnBreach)
        };
    }
}
